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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Policy;
using System.Text;
using AppSecInc.ProcessDomain.Remoting;

namespace AppSecInc.ProcessDomain
{
    /// <summary>
    /// Parameters for a process domain
    /// </summary>
    [Serializable]
    public class ProcessDomainSetup
    {
        public const string DefaultCompilerVersion = "v3.5";

        public ProcessDomainSetup()
        {
            ExecutableDirectory = Path.GetTempPath();
            ProcessStartTimeout = new TimeSpan(0, 0, 60);
            FileDeletionTimeout = new TimeSpan(0, 0, 10);
            DeleteOnUnload = true;
            RestartOnProcessExit = true;
            AppDomainSetupInformation = AppDomain.CurrentDomain.SetupInformation;
            WorkingDirectory = Environment.CurrentDirectory;
            Evidence = AppDomain.CurrentDomain.Evidence;
            TypeFilterLevel = TypeFilterLevel.Low;
            DllDirectory = CurrentDllDirectory;
            EnvironmentVariables = new Dictionary<string, string>();
            ExternalAssemblies = new Dictionary<AssemblyName, string>();
            PriorityClass = ProcessPriorityClass.Normal;
            CompilerVersion = DefaultCompilerVersion;
            CopyConfigurationFile = false;
            Remoting = new RemotingOptions();
        }

        /// <summary>
        /// Determines whether the AppDomainSetup configuration file has to be copied into assembly name dot config file.
        /// </summary>
        public bool CopyConfigurationFile { get; set; }

        /// <summery>
        /// Specifies the c# compiler version for the remote process
        /// </summery>
        public string CompilerVersion { get; set; }

        /// <summary>
        /// Specifies maximum time spent trying to delete a assembly from the disk.
        /// </summary>
        public TimeSpan FileDeletionTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies where the temporary remote process executable file will be created
        /// </summary>
        public string ExecutableDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies the working directory for the remote process
        /// </summary>
        public string WorkingDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies a directory to invoke SetDllDirectory with to redirect DLL probing to the working directory
        /// </summary>
        public string DllDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the currently configured DLL search path as set by SetDllDirectory
        /// </summary>

        public static string CurrentDllDirectory
        {
            get 
            {
                int bytesNeeded = WinApi.GetDllDirectory(0, null);
                if (bytesNeeded == 0)
                {
                    throw new Win32Exception();
                }
                var sb = new StringBuilder(bytesNeeded);
                // reset the last error on this thread
                WinApi.SetLastError(0);
                bytesNeeded = WinApi.GetDllDirectory(bytesNeeded, sb);
                
                // does 0 mean failure or empty string?
                if (bytesNeeded == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    if (errorCode != 0)
                    {
                        throw new Win32Exception(errorCode);
                    }
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Specifies how long to wait for the remote process to start
        /// </summary>
        public TimeSpan ProcessStartTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies whether or not to delete the generated executable after the process domain has unloaded
        /// </summary>
        public bool DeleteOnUnload
        {
            get;
            set;
        }

        /// <summary>
        /// Specifices whether the process domain process should be relaunched should the process exit prematurely
        /// </summary>
        public bool RestartOnProcessExit
        {
            get;
            set;
        }

        /// <summary>
        /// Setup information for the AppDomain that the object will be created in, in the remote process.  By default, this will be the
        /// current domain's setup information from which the proxy is being created
        /// </summary>
        public AppDomainSetup AppDomainSetupInformation
        {
            get;
            set;
        }

        /// <summary>
        /// Allows specifying which platform to compile the target remote process assembly for
        /// </summary>
        public PlatformTarget Platform
        {
            get;
            set;
        }

        /// <summary>
        /// Remote security policy
        /// </summary>
        public Evidence Evidence
        {
            get;
            set;
        }


        /// <summary>
        /// Remoting type filter level - controls how much functionality is exposed via remoting the process domain remotely
        /// </summary>
        public TypeFilterLevel TypeFilterLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Environment variables of the remote process
        /// </summary>
        public Dictionary<string,string> EnvironmentVariables
        {
            get;
            set;
        }

        /// <summary>
        /// A map of assembly names to assembly file locations that will need to be resolved inside the Process Domain
        /// </summary>
        public Dictionary<AssemblyName, string> ExternalAssemblies 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// The priority to run the remote process at
        /// </summary>
        public ProcessPriorityClass PriorityClass 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Advanced remoting-related options
        /// </summary>
        public RemotingOptions Remoting { get; private set; }

        internal static void Serialize(ProcessDomainSetup setup, string filename)
        {
            var formatter = new BinaryFormatter();

            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                formatter.Serialize(fs, setup);
            }
        }

        internal static ProcessDomainSetup Deserialize(string filename)
        {
            var formatter = new BinaryFormatter();

            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return (ProcessDomainSetup)formatter.Deserialize(fs);
            }
        }
    }
}
