/*******************************************************************************
* ProcessDomain (http://processdomain.codeplex.com)
* 
* Copyright (c) 2011 Application Security, Inc.
* 
* All rights reserved. This program and the accompanying materials
* are made available under the terms of the Eclipse Public License v1.0
* which accompanies this distribution, and is available at
* http://www.eclipse.org/legal/epl-v10.html
*
* Contributors:
*     Application Security, Inc.
*******************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters;
using System.Security;
using System.Threading;
using AppSecInc.ProcessDomain.Remoting;
using AppSecInc.ProcessDomain.Utils;
using NUnit.Framework;

namespace AppSecInc.ProcessDomain.UnitTests
{
    [TestFixture]
    public class TestProcessDomain
    {
        static readonly string TestObjectAssemblyName = typeof(RemoteTestObject).Assembly.FullName;
        static readonly string TestObjectTypeName = typeof(RemoteTestObject).FullName;

        [Test]
        public void TestFriendlyName()
        {
            using (var domain = ProcessDomain.CreateDomain("ProcessDomain"))
            {
                var obj = (RemoteTestObject)domain.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                Assert.That(obj.GetProcessId(), Is.Not.EqualTo(Process.GetCurrentProcess().Id));
                Assert.That(obj.GetProcessFileName(), Is.Not.EqualTo(Process.GetCurrentProcess().MainModule.FileName));
                Assert.That(obj.GetProcessFileName().EndsWith("ProcessDomain.exe"));
            }
        }

        [Test]
        public void TestDomainAttachDetach()
        {
            var attachedEvent = new ManualResetEvent(false);
            var detachedEvent = new ManualResetEvent(false);

            using (var domain = ProcessDomain.CreateDomain("ProcessDomain"))
            {
                domain.Attached += () => attachedEvent.Set();
                domain.Detached += () => detachedEvent.Set();

                var obj = (RemoteTestObject)domain.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                Assert.That(!attachedEvent.WaitOne(0));
                Assert.That(!detachedEvent.WaitOne(0));

                // restart should occur, but our current object will be invalid
                Process.GetProcessById(obj.GetProcessId()).Kill();
                Assert.That(detachedEvent.WaitOne(10000), "Timed-out waiting for process to die");
                Assert.That(attachedEvent.WaitOne(10000), "Timed-out waiting for process to respawn");
                Assert.Throws<RemotingException>(() => obj.GetProcessId());

                // create object in restarted domain
                obj = (RemoteTestObject)domain.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                Assert.That(obj.GetProcessId(), Is.Not.EqualTo(Process.GetCurrentProcess().Id));

            }

            var setupInfo = new ProcessDomainSetup
            {
                RestartOnProcessExit = false
            };

            // now restart should not occur
            using (var domain = ProcessDomain.CreateDomain("RemoteProcess2", setupInfo))
            {
                domain.Attached += () => attachedEvent.Set();
                domain.Detached += () => detachedEvent.Set();

                var obj = (RemoteTestObject)domain.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                Assert.That(obj.GetProcessId(), Is.Not.EqualTo(Process.GetCurrentProcess().Id));

                attachedEvent.Reset();
                detachedEvent.Reset();

                Process.GetProcessById(obj.GetProcessId()).Kill();

                Assert.That(detachedEvent.WaitOne(10000), "Timed-out waiting for process to die");
                Assert.That(!attachedEvent.WaitOne(5000), "Unexpected re-attach");
                Assert.Throws<RemotingException>(() => obj.GetProcessId());
                Assert.Throws<RemotingException>(() => obj = (RemoteTestObject)domain.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName));
            }
        }

        [Test]
        public void TestDefaultWorkingDirectory()
        {
            using (var domain = ProcessDomain.CreateDomain("ProcessDomain"))
            {
                var obj = (RemoteTestObject)domain.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                Assert.That(obj.CurrentDirectory, Is.EqualTo(Environment.CurrentDirectory));
            }
        }

        [Test]
        public void TestExecutableLocation()
        {
            string desiredExecutableFileName = Path.Combine(Environment.CurrentDirectory, "MyDomain.exe");

            var setupInfo = new ProcessDomainSetup
            {
                ExecutableDirectory = Environment.CurrentDirectory
            };

            // default uses temp directory
            using (var domain1 = ProcessDomain.CreateDomain("MyDomain"))
            {
                var obj = (RemoteTestObject)domain1.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                Assert.That(obj.GetProcessFileName(), Is.Not.EqualTo(desiredExecutableFileName));
            }

            // now using our specified location
            using (var domain2 = ProcessDomain.CreateDomain("MyDomain", setupInfo))
            {
                var obj = (RemoteTestObject)domain2.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                Assert.That(obj.GetProcessFileName(), Is.EqualTo(desiredExecutableFileName));
            }

            // test if file exists, it will be overwritten
            using (var writer = new StreamWriter(desiredExecutableFileName, false))
            {
                writer.WriteLine("Garbage");

                // will fail to compile because file is open
                Assert.Throws<AssemblyGeneratorCompilerException>(() => ProcessDomain.CreateDomain("MyDomain", setupInfo));

                writer.Flush();
            }

            // file is now closed, but contains garbage that can't execute,
            // but the file will get overwritten
            using (var domain3 = ProcessDomain.CreateDomain("MyDomain", setupInfo))
            {
                var obj = (RemoteTestObject)domain3.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                Assert.That(obj.GetProcessFileName(), Is.EqualTo(desiredExecutableFileName));
            }

            // and once we're done, file is gone
            Assert.That(!File.Exists(desiredExecutableFileName));
        }

        [Test]
        public void TestDeleteFileOnExit()
        {
            string desiredExecutableFileName = Path.Combine(Environment.CurrentDirectory, "ProcessDomain.exe");

            var setupInfo = new ProcessDomainSetup
            {
                ExecutableDirectory = Environment.CurrentDirectory,
                DeleteOnUnload = false
            };

            ProcessDomain.Unload(ProcessDomain.CreateDomain("ProcessDomain", setupInfo));
            Assert.That(File.Exists(desiredExecutableFileName));

            setupInfo.DeleteOnUnload = true;

            ProcessDomain.Unload(ProcessDomain.CreateDomain("ProcessDomain", setupInfo));
            Assert.That(!File.Exists(desiredExecutableFileName));
        }

        [Test]
        public void TestConfigurationLocation()
        {
            // by default uses our app config
            using (var domain1 = ProcessDomain.CreateDomain("Domain1"))
            {
                var obj = (RemoteTestObject)domain1.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                Assert.That(obj.GetAppConfigValue("MyValue"), Is.EqualTo("MyValue"));
            }

            // now point it at a different app config
            var setupInfo = new ProcessDomainSetup
            {
                AppDomainSetupInformation =
                {
                    ConfigurationFile = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile), "OtherApp.config")
                }
            };

            using (var domain2 = ProcessDomain.CreateDomain("Domain2", setupInfo))
            {
                var obj = (RemoteTestObject)domain2.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                Assert.That(obj.GetAppConfigValue("MyValue"), Is.EqualTo("OtherValue"));
            }
        }

        [Test]
        public void TestProcessDomainAssemblyResolver()
        {
            string prevDirectory = Environment.CurrentDirectory;

            try
            {
                Directory.SetCurrentDirectory(Path.Combine(prevDirectory, ".."));

                using (var domain = ProcessDomain.CreateDomain("ProcessDomain"))
                {
                    // this used to fail because it would try to load the ProcessDomain assembly from 'current directory'
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(prevDirectory);
            }
        }
		
		[Test]
		public void TestAssemblyUtils()
		{
			string uriPathUnescaped = "file:///c:/somepath/i have spaces";
			string uriPathEscaped = "file:///c:/somepath/i%20have%20spaces";
			string forwardSlashPath = "c:/somepath/i have spaces";
			string path = @"c:\somepath\i have spaces";
			
			Assert.That(AssemblyUtils.GetFilePathFromFileUri(path), Is.EqualTo(path));
			Assert.That(AssemblyUtils.GetFilePathFromFileUri(forwardSlashPath), Is.EqualTo(path));
			Assert.That(AssemblyUtils.GetFilePathFromFileUri(uriPathUnescaped), Is.EqualTo(path));
			Assert.That(AssemblyUtils.GetFilePathFromFileUri(uriPathEscaped), Is.EqualTo(path));
		}
        [Test]
        public void TestTypeFilterLevel()
        {
            // using an event handler requires type filter level Full for a remoting channel

            // by default, it's low, which will fail
            using (var domain = ProcessDomain.CreateDomain("Domain"))
            {
                var obj = (RemoteTestObject)domain.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);

                Assert.Throws<SecurityException>(()=> obj.CallbackEvent += () => Assert.Fail("This should have failed"));
            }

            // now enable the remoting channel with type filter level Full
            var setup = new ProcessDomainSetup { TypeFilterLevel = TypeFilterLevel.Full };
            using (var domain = ProcessDomain.CreateDomain("Domain", setup))
            {
                var obj = (RemoteTestObject)domain.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                
                obj.CalledBack = false;
                Assert.That(!obj.CalledBack);
                Assert.DoesNotThrow(() => obj.CallbackEvent += obj.SetCalledBack);
                obj.OnCallback();
                Assert.That(obj.CalledBack);
            }
        }

        [Test]
        public void TestMultipleProcessDomains()
        {
            // Along with enabling support for TypeFilterLevel = Full, we also have to allow multiple
            // channels to be created.  This will simply do just that and ensure there's no 
            // duplicate channel registration exceptions
            using (var domain1 = ProcessDomain.CreateDomain("Domain1"))
            using (var domain2 = ProcessDomain.CreateDomain("Domain2"))
            {
                var obj1 = (RemoteTestObject)domain1.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);
                var obj2 = (RemoteTestObject)domain2.CreateInstanceAndUnwrap(TestObjectAssemblyName, TestObjectTypeName);

                Assert.That(!obj1.CalledBack);
                Assert.That(!obj2.CalledBack);
                obj1.SetCalledBack();
                obj2.SetCalledBack();
                Assert.That(obj1.CalledBack);
                Assert.That(obj2.CalledBack);
            }
        }

        [Test]
        public void TestProcessPriority()
        {
            // Default case
            using (var domain = ProcessDomain.CreateDomain("TestPriorityDomain"))
            {
                var obj = (RemoteTestObject)domain.CreateInstanceAndUnwrap(typeof(RemoteTestObject).Assembly.FullName, typeof(RemoteTestObject).FullName);
                Assert.That(obj.GetPriority(), Is.EqualTo(ProcessPriorityClass.Normal));
            }

            // Try each priority
            foreach (ProcessPriorityClass priority in Enum.GetValues(typeof(ProcessPriorityClass)))
            {
                var setup = new ProcessDomainSetup { PriorityClass = priority };
                using (var domain = ProcessDomain.CreateDomain("TestPriorityDomain", setup))
                {
                    var obj = (RemoteTestObject)domain.CreateInstanceAndUnwrap(typeof(RemoteTestObject).Assembly.FullName, typeof(RemoteTestObject).FullName);
                    Assert.That(obj.GetPriority(), Is.EqualTo(priority));
                }
            }
        }
    }
}
