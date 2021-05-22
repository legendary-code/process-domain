using System;
using System.IO;
using log4net.Config;

namespace AppSecInc.ProcessDomain.Utils
{
    /// <summary>
    /// Utility class for configuring log4net remotely in another process domain
    /// </summary>
    [Serializable]
    public class LoggingConfigurator : MarshalByRefObject
    {
        public static LoggingConfigurator CreateConfigurator(ProcessDomain domain)
        {
            return (LoggingConfigurator)domain.CreateInstanceAndUnwrap(typeof(LoggingConfigurator).Assembly.FullName, typeof(LoggingConfigurator).FullName);
        }

        public void ConfigureBasic()
        {
            BasicConfigurator.Configure();
        }

        public void ConfigureAndWatchAppConfig()
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));
        }

        public void ConfigureAppConfig()
        {
            XmlConfigurator.Configure(new FileInfo(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));
        }
    }
}
