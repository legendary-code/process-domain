using System;
using System.Configuration;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;

namespace AppSecInc.ProcessDomain.UnitTests
{
    [Serializable]
    public class RemoteTestObject : MarshalByRefObject
    {
        public delegate void Callback();
        public event Callback CallbackEvent;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public string GetProcessFileName()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        public int GetProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        public string CurrentDirectory
        {
            get { return Environment.CurrentDirectory; }
        }

        public string GetAppConfigValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public ProcessPriorityClass GetPriority()
        {
            return Process.GetCurrentProcess().PriorityClass;
        }

        public bool RunningAsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public void OnCallback()
        {
            if (CallbackEvent != null)
            {
                CallbackEvent();
            }
        }

        public bool CalledBack { get; set; }

        /// <summary>
        /// This method is used for CallbackEvent because the target method needs to be in a class
        /// that is also serializable for subscribing to events to work
        /// </summary>
        public void SetCalledBack()
        {
            CalledBack = true;
        }
    }
}
