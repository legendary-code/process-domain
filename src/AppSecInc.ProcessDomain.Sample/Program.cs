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
using System.Linq;
using System.Threading;
using AppSecInc.ProcessDomain.Utils;

namespace AppSecInc.ProcessDomain.Sample
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var localObject = new RemoteObject();
                Console.WriteLine("Local object - Main Module Location: {0}", localObject.GetProcessMainModuleFileName());
                Console.WriteLine("Local object - App.config location: {0}", localObject.GetAppConfigLocation());
                Console.WriteLine("Local object - App.config value: {0}", localObject.GetAppConfigValue("MyString"));

                var setup = new ProcessDomainSetup
                {
                    ProcessStartTimeout = new TimeSpan(0, 0, 5),
                };

                using (ProcessDomain processDomain = ProcessDomain.CreateDomain("RemoteProcess", setup))
                {
                    LoggingConfigurator.CreateConfigurator(processDomain).ConfigureAppConfig();                   
                    
                    var remoteObject = (RemoteObject)processDomain.CreateInstanceAndUnwrap(typeof(RemoteObject).Assembly.FullName, typeof(RemoteObject).FullName);
                    Console.WriteLine("Remote object - Main Module Location: {0}", remoteObject.GetProcessMainModuleFileName());
                    Console.WriteLine("Remote object - App.config location: {0}", remoteObject.GetAppConfigLocation());
                    Console.WriteLine("Remote object - App.config value: {0}", remoteObject.GetAppConfigValue("MyString"));

                    var detachedEvent = new ManualResetEvent(false);
                    var attachedEvent = new ManualResetEvent(false);
                    processDomain.Detached += () => detachedEvent.Set();
                    processDomain.Attached += () =>
                    {
                        LoggingConfigurator.CreateConfigurator(processDomain).ConfigureAppConfig();
                        attachedEvent.Set();
                    };

                    Console.WriteLine("Finding RemoteProcess and killing it...");
                    Process.GetProcessesByName("RemoteProcess").FirstOrDefault().Kill();
                    
                    if (!detachedEvent.WaitOne(10000))
                    {
                        throw new Exception("Timed-out while waiting for process to die");
                    }

                    Console.WriteLine("Waiting for new process to spawn");
                    if (!attachedEvent.WaitOne(10000))
                    {
                        throw new Exception("Timed-out while waiting for process to restart");
                    }

                    Console.WriteLine("Re-creating remote object in newly spawned process");
                    remoteObject = (RemoteObject)processDomain.CreateInstanceAndUnwrap(typeof(RemoteObject).Assembly.FullName, typeof(RemoteObject).FullName);
                    Console.WriteLine("Remote object - Main Module Location: {0}", remoteObject.GetProcessMainModuleFileName());
                    Console.WriteLine("Remote object - App.config location: {0}", remoteObject.GetAppConfigLocation());
                    Console.WriteLine("Remote object - App.config value: {0}", remoteObject.GetAppConfigValue("MyString"));

                    Console.WriteLine("Throwing an exception...");
                    try
                    {
                        remoteObject.ThrowException();
                        Console.WriteLine("Did not catch an exception...");
                    }
                    catch (RemoteException rex)
                    {
                        Console.WriteLine("Caught exception: {0}", rex.Message);
                    }
                }

                Console.WriteLine("Two process domains at the same time");

                using (var processDomain1 = ProcessDomain.CreateDomain("RemoteProcess1", setup))
                using (var processDomain2 = ProcessDomain.CreateDomain("RemoteProcess2", setup))
                {
                    var remoteObject1 = (RemoteObject)processDomain1.CreateInstanceAndUnwrap(typeof(RemoteObject).Assembly.FullName, typeof(RemoteObject).FullName);
                    var remoteObject2 = (RemoteObject)processDomain2.CreateInstanceAndUnwrap(typeof(RemoteObject).Assembly.FullName, typeof(RemoteObject).FullName);

                    Console.WriteLine("Remote object #1 - App.config value: {0}", remoteObject1.GetAppConfigValue("MyString"));
                    Console.WriteLine("Remote object #2 - App.config value: {0}", remoteObject2.GetAppConfigValue("MyString"));
                }

                Console.WriteLine("Process domain in alternate location");
                setup.AppDomainSetupInformation.ApplicationBase = @"c:\";
                setup.ExternalAssemblies[typeof(Program).Assembly.GetName()] = typeof(Program).Assembly.Location;
                using (var processDomain = ProcessDomain.CreateDomain("RemoteProcess", setup))
                {
                    var remoteObject = (RemoteObject)processDomain.CreateInstanceAndUnwrap(typeof(RemoteObject).Assembly.FullName, typeof(RemoteObject).FullName);

                    Console.WriteLine("Remote object - App.config value: {0}", remoteObject.GetAppConfigValue("MyString"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex);
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
