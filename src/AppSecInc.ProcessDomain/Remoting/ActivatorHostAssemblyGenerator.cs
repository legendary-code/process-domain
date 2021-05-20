using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using log4net;
using Microsoft.CSharp;

namespace AppSecInc.ProcessDomain.Remoting
{
    /// <summary>
    /// Generates an assembly to run in a separate process in order to host an Activator
    /// </summary>
    internal static class ActivatorHostAssemblyGenerator
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ActivatorHostAssemblyGenerator));
        const string AssemblyName = @"{0}.exe";
        private static readonly String[] ReferencedAssemblies = new[] { "System.dll" };

        public static string CreateRemoteHostAssembly(string friendlyName, ProcessDomainSetup setupInfo)
        {
            if (!Directory.Exists(setupInfo.ExecutableDirectory))
            {
                Directory.CreateDirectory(setupInfo.ExecutableDirectory);
            }

            var providerOptions = new Dictionary<string,string>
                                      {
                                          { "CompilerVersion", setupInfo.CompilerVersion ?? ProcessDomainSetup.DefaultCompilerVersion }
                                      };

            var provider = new CSharpCodeProvider(providerOptions);

            var compilerArgs = new List<string> {PlatformTargetUtil.GetCompilerArgument(setupInfo.Platform)};

            var compilerParameters = new CompilerParameters
                                      {
                                          GenerateExecutable = true,
                                          GenerateInMemory = false,
                                          CompilerOptions = string.Join(" ", compilerArgs.ToArray()),
                                          OutputAssembly = Path.Combine(setupInfo.ExecutableDirectory, 
                                                                        string.Format(AssemblyName, friendlyName))
                                      };

            compilerParameters.ReferencedAssemblies.AddRange(ReferencedAssemblies);

            string assemblySource = Properties.Resources.Program
                                                            .Replace("${ActivatorHostTypeName}", typeof (ActivatorHost).AssemblyQualifiedName)
                                                            .Replace("${ProcessDomainAssemblyName}", typeof(ActivatorHost).Assembly.FullName);

            var results = provider.CompileAssemblyFromSource(compilerParameters, assemblySource);

            if (results.Errors.HasErrors)
            {
                throw new AssemblyGeneratorCompilerException("Failed to compile assembly for process domain due to compiler errors", results.Errors);
            }

            if (results.Errors.HasWarnings)
            {
                Logger.Warn("Process domain assembly compilation returned with warnings:");
                foreach (var error in results.Errors)
                {
                    Logger.WarnFormat("Compiler Warning: {0}", error);
                }
            }
            if (setupInfo.CopyConfigurationFile)
            {
                File.Copy(setupInfo.AppDomainSetupInformation.ConfigurationFile,
                          results.PathToAssembly + ".config", true);
            }
            return results.PathToAssembly;
        }
    }
}