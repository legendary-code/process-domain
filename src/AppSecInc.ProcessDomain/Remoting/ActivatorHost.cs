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
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;

namespace AppSecInc.ProcessDomain.Remoting
{
    /// <summary>
    /// This class hosts an Activator in a new process on an IPC channel
    /// </summary>
    [Serializable]
    internal class ActivatorHost : MarshalByRefObject
    {
        public const string ServerChannelName = "ProcessDomainServer_{0}";
        public const string ClientChannelName = "ProcessDomainClient_{0}";
        public const string EventName = "ProcessDomainEvent_{0}";
        public const string ActivatorName = "Activator";

        readonly Process _process;
        readonly IpcChannel _channel;

        public ActivatorHost(string guid, int processId, ProcessDomainSetup setup)
        {
            SetupDllDirectories(setup);
            var serverProvider = new BinaryServerFormatterSinkProvider { TypeFilterLevel = setup.TypeFilterLevel };
            var clientProvider = new BinaryClientFormatterSinkProvider();
            _process = Process.GetProcessById(processId);

            var properties = new Hashtable();
            properties["portName"] = string.Format(ServerChannelName, guid);
            properties["name"] = string.Format(ServerChannelName, guid);
            properties["rejectRemoteRequests"] = true;
            setup.Remoting.ApplyServerProperties(properties);

            _channel = new IpcChannel(properties, clientProvider, serverProvider);
            ChannelServices.RegisterChannel(_channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(Activator), ActivatorName, WellKnownObjectMode.Singleton);

            EventWaitHandle serverStartedHandle = null;
            try
            {
                bool created;
                serverStartedHandle = new EventWaitHandle(false, EventResetMode.ManualReset, string.Format(EventName, guid), out created);

                if (created)
                {
                    throw new Exception("Event handle did not exist for remote process");
                }

                serverStartedHandle.Set();
            }
            finally
            {
                if (serverStartedHandle != null)
                {
                    serverStartedHandle.Close();
                }
            }
        }

        /// <summary>
        /// Waits for the parent process to exit
        /// </summary>
        public void WaitForExit()
        {
            _process.WaitForExit();
        }

        /// <summary>
        /// Runs the Activator Host and blocks until the parent process exits
        /// </summary>
        public static void Run(string[] args)
        {
            // args[0] = process domain assembly path
            // args[1] = guid
            // args[2] = parent process id
            // args[3] = ProcessDomainSetup file

            if (args.Length != 4)
            {
                return;
            }

            string friendlyName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            string guid = args[1];
            int processId = int.Parse(args[2]);
            
            var domainSetup = ProcessDomainSetup.Deserialize(args[3]);
            
            var domain = AppDomain.CreateDomain(friendlyName, domainSetup.Evidence, domainSetup.AppDomainSetupInformation);

            var type = Assembly.GetEntryAssembly().GetType("AppSecInc.ProcessDomain.AssemblyResolver");

            if (type == null)
            {
                throw new TypeLoadException("Could not load type for assembly resolver");
            }

            // add ProcessDomain assembly to resolver
            if (domainSetup.ExternalAssemblies == null)
            {
                domainSetup.ExternalAssemblies = new System.Collections.Generic.Dictionary<AssemblyName, string>();
            }
            domainSetup.ExternalAssemblies[typeof(ActivatorHost).Assembly.GetName()] = typeof(ActivatorHost).Assembly.Location;           
            
            var resolver = domain.CreateInstanceFromAndUnwrap(type.Assembly.Location, 
                                            type.FullName, 
                                            false, 
                                            BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance, 
                                            null, 
                                            new[] { domainSetup.ExternalAssemblies }, 
                                            null, null, null);

            type.InvokeMember("Setup", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public, null, resolver, null);

            var host = (ActivatorHost) domain.CreateInstanceFromAndUnwrap(typeof (ActivatorHost).Assembly.Location,
                                                                          typeof (ActivatorHost).FullName,
                                                                          false,
                                                                          BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance,
                                                                          null,
                                                                          new object[] {guid, processId, domainSetup},
                                                                          null, null, null);

            host.WaitForExit();

            type.InvokeMember("TearDown", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public, null, resolver, null);

            // If parent process (host) finishes, the current process must end.
            Environment.Exit(0);
        }

        private static void SetupDllDirectories(ProcessDomainSetup setup)
        {
            if (!string.IsNullOrEmpty(setup.DllDirectory))
            {
                if (!WinApi.SetDllDirectory(setup.DllDirectory))
                {
                    throw new Win32Exception();
                }
            }
        }
    }
}
