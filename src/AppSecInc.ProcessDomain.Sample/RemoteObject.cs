using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using log4net;
using log4net.Config;

namespace AppSecInc.ProcessDomain.Sample
{
    [Serializable]
    public class RemoteObject : MarshalByRefObject
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RemoteObject));

        public void ConfigLogging()
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));
        }

        public string GetProcessMainModuleFileName()
        {
            Logger.Info("Called GetProcessMainModuleFileName()");
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        public string GetAppConfigLocation()
        {
            Logger.Info("Called GetAppConfigLocation()");
            return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
        }

        public string GetAppConfigValue(string key)
        {
            Logger.Info("Called GetAppConfigValue()");
            return ConfigurationManager.AppSettings[key];
        }

        public void ThrowException()
        {
            throw new RemoteException("This is an exception");
        }
    }
}
