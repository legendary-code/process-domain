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
using System.Configuration;
using System.Diagnostics;

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
