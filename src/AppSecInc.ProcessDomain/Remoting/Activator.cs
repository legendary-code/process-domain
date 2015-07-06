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

namespace AppSecInc.ProcessDomain.Remoting
{
    /// <summary>
    /// This is a remotable object responsible for creating instances of objects remotely in another process
    /// </summary>
    [Serializable]
    internal class Activator : MarshalByRefObject, IActivation
    {
        // Make sure that the object instance is never garbage collected because
        // it's not gc-rooted
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public object CreateInstanceAndUnwrap(string assemblyName, string typeName)
        {
            return AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName, typeName);
        }

        public object CreateInstanceAndUnwrap(string assemblyName, string typeName, object[] activationAttributes)
        {
            return AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName, typeName, activationAttributes);
        }

        public object CreateInstanceAndUnwrap(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes)
        {
            return AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
        }
    }
}
