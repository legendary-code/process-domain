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
