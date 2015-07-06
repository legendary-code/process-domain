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
using System.Diagnostics;
using System.IO;
using System.Reflection;

/*
 * This file contains source code that will get compiled at run-time in order to create a
 * hosting process.  It should remain as light-weight as possible, while the bulk of the
 * work should be done in the created type 'ActivatorHost'.  It should also not have any
 * references to assemblies not in the GAC, as this program will fail to load because the
 * assembly resolver won't have a chance to get configured to find assemblies in a different
 * location than the application domain's Base Directory.
 */

[assembly: AssemblyTitle("ProcessDomain Host")]
[assembly: AssemblyDescription("Provides an isolated environment for creating objects and executing code in another process")]

namespace AppSecInc.ProcessDomain
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Log("Invalid arguments");
                return;
            }

            try
            {
                var resolveMap = new Dictionary<AssemblyName, string>
                {
                    { new AssemblyName("${ProcessDomainAssemblyName}"), args[0] }
                };
                var resolver = new AssemblyResolver(resolveMap);
                resolver.Setup();

                Type hostType = Type.GetType("${ActivatorHostTypeName}");

                if (hostType == null)
                {
                    throw new TypeLoadException(string.Format("Could not load ActivatorHost type '${ActivatorHostTypeName}' using resolver with '${ProcessDomainAssemblyName}' mapped to '{0}'", args[0]));
                }

                var methodInfo = hostType.GetMethod("Run", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string[]) }, null);

                if(methodInfo == null)
                {
                    throw new Exception("'Run' method on ActivatorHost not found.");
                }

                methodInfo.Invoke(null, new[] { args });
            }
            catch (Exception ex)
            {
                Log("Failed to launch Activator Host: {0}", ex);
            }
        }

        // Just very basic logging.  Any extra logging configuration should be done by creating
        // the logging configurator object in the new process domain
        static StreamWriter _logFile;
        static void OpenLogFile()
        {
            string fileName = string.Format("{0}-{1}.log", Assembly.GetEntryAssembly().Location, Process.GetCurrentProcess().Id);
            try
            {
                _logFile = new StreamWriter(fileName, false);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to open process domain bootstrap log: {0}", ex);
            }
        }

        static void Log(string message, params object[] args)
        {
            if (_logFile == null)
            {
                OpenLogFile();
            }

            if (_logFile != null)
            {
                _logFile.WriteLine(string.Format("[{0}] {1}", DateTime.Now, message), args);
                _logFile.Flush();
            }
        }
    }

    public class AssemblyResolver : MarshalByRefObject
    {
        // Prevent garbage collection
        public override object InitializeLifetimeService()
        {
            return null;
        }

        private readonly Dictionary<string, Dictionary<AssemblyName, string>> _mapByName;

        public AssemblyResolver(Dictionary<AssemblyName, string> map)
        {
            _mapByName = new Dictionary<string, Dictionary<AssemblyName, string>>();

            if (map != null)
            {
                foreach (var entry in map)
                {
                    Dictionary<AssemblyName, string> subMap;
                    if (!_mapByName.TryGetValue(entry.Key.Name, out subMap))
                    {
                        _mapByName[entry.Key.Name] = subMap = new Dictionary<AssemblyName, string>();
                    }

                    subMap[entry.Key] = entry.Value;
                }
            }
        }

        public void Setup()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyResolve;
        }

        public void TearDown()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= ReflectionOnlyResolve;
        }

        private Assembly Resolve(object sender, ResolveEventArgs args)
        {
            var assemblyFile = FindAssemblyName(args.Name);
            if (assemblyFile != null)
            {
                return Assembly.LoadFrom(assemblyFile);
            }
            
            return null;
        }

        private Assembly ReflectionOnlyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyFile = FindAssemblyName(args.Name);
            if (assemblyFile != null)
            {
                return Assembly.ReflectionOnlyLoadFrom(assemblyFile);
            }

            return null;
        }

        private static bool PublicKeysTokenEqual(byte[] lhs, byte[] rhs)
        {
            if (lhs == null || rhs == null)
            {
                return lhs == rhs;
            }

            if (lhs.Length != rhs.Length)
                return false;

            for (int i = 0; i < lhs.Length; i++)
            {
                if (lhs[i] != rhs[i])
                    return false;
            }

            return true;
        }

        private string FindAssemblyName(string name)
        {
            var assemblyName = new AssemblyName(name);
            Dictionary<AssemblyName, string> subMap;

            if (!_mapByName.TryGetValue(assemblyName.Name, out subMap))
            {
                return null;
            }

            foreach (var entry in subMap)
            {
				// do weak assembly name matching, matching only values specified in assembly name
                if (assemblyName.Version != null && assemblyName.Version != entry.Key.Version)
                    continue;

                if (assemblyName.CultureInfo != null && !assemblyName.CultureInfo.Equals(entry.Key.CultureInfo))
                    continue;

                if (!PublicKeysTokenEqual(assemblyName.GetPublicKeyToken(), entry.Key.GetPublicKeyToken()))
                    continue;

                return entry.Value;
            }

            return null;
        }
    }
}