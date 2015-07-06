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
using System.Globalization;
using System.Reflection;
using System.Security.Policy;
using System.Threading;

using log4net;

using AppSecInc.ProcessDomain.Remoting;

namespace AppSecInc.ProcessDomain
{
    /// <summary>
    /// Represents an isolated environment in a separate process in which objects can be created and invoked
    /// </summary>
    public sealed class ProcessDomain : IActivation, IDisposable
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ProcessDomain));

        public event AttachedDelegate Attached;
        public event DetachedDelegate Detached;

        readonly AutoResetEvent _attachedEvent = new AutoResetEvent(false);
        readonly AutoResetEvent _detachedEvent = new AutoResetEvent(false);
        readonly string _friendlyName;
        readonly ActivatorProcess _process;

        int _unloaded;

        private ProcessDomain(string friendlyName, ProcessDomainSetup setupInfo)
        {
            _friendlyName = friendlyName;
            _process = new ActivatorProcess(friendlyName, setupInfo);
            _process.Attached += Process_Attached;
            _process.Detached += Process_Detached;
            _process.Start();
        }

        private void Process_Attached()
        {
            var tmp = Attached;
            if (tmp != null)
            {
                Attached();
            }
            _attachedEvent.Set();
        }

        private void Process_Detached()
        {
            var tmp = Detached;
            if (tmp != null)
            {
                Detached();
            }
            _detachedEvent.Set();
        }

        /// <summary>
        /// Creates a ProcessDomain which allows hosting objects and code out-of-process
        /// </summary>
        /// <param name="friendlyName">The friendly name of the process domain which directly will also be the file name of the remote process</param>
        /// <param name="setupInfo">Additional settings for creating the process domain</param>
        public static ProcessDomain CreateDomain(string friendlyName, ProcessDomainSetup setupInfo)
        {           
            Logger.InfoFormat("Creating process domain '{0}'", friendlyName);
            return new ProcessDomain(friendlyName, setupInfo);
        }

        /// <summary>
        /// Creates a ProcessDomain which allows hosting objects and code out-of-process
        /// </summary>
        /// <param name="friendlyName">The friendly name of the process domain which directly will also be the file name of the remote process</param>
        public static ProcessDomain CreateDomain(string friendlyName)
        {
            return CreateDomain(friendlyName, new ProcessDomainSetup());
        }

        /// <summary>
        /// Unloads a given process domain by terminating the process
        /// </summary>
        /// <param name="domain">The process domain to unload</param>
        public static void Unload(ProcessDomain domain)
        {
            Logger.InfoFormat("Unloading process domain '{0}'", domain._friendlyName);
            domain.Unload();
        }

        /// <summary>
        /// Creates an object of the specified type
        /// </summary>
        public object CreateInstanceAndUnwrap(string assemblyName, string typeName)
        {
            return _process.Activator.CreateInstanceAndUnwrap(assemblyName, typeName);
        }

        /// <summary>
        /// Creates an object of the specified type
        /// </summary>
        public object CreateInstanceAndUnwrap(string assemblyName, string typeName, object[] activationAttributes)
        {
            return _process.Activator.CreateInstanceAndUnwrap(assemblyName, typeName, activationAttributes);
        }

        /// <summary>
        /// Creates an object of the specified type
        /// </summary>
        public object CreateInstanceAndUnwrap(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes)
        {
            return _process.Activator.CreateInstanceAndUnwrap(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
        }

        /// <summary>
        /// Terminates the process, and then right after that it restarts again.
        /// </summary>
        public void Terminate()
        {
            if (_process != null)
            {
                _process.Terminate();
            }
        }

        private void Unload()
        {
            if (Interlocked.CompareExchange(ref _unloaded, 1, 0) == 1)
                return;

            if (_process != null)
            {
                _process.Kill();
                _process.Dispose();
            }
        }

        ~ProcessDomain()
        {
            Unload();
        }

        void IDisposable.Dispose()
        {
            Unload();
        }
    }
}