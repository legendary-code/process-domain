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

using System.Globalization;
using System.Reflection;
using System.Security.Policy;

namespace AppSecInc.ProcessDomain.Remoting
{
    /// <summary>
    /// Interface that the activator and process domain must implement to expose methods for creating object instances in a remote process
    /// </summary>
    public interface IActivation
    {
        object CreateInstanceAndUnwrap(string assemblyName, string typeName);
        object CreateInstanceAndUnwrap(string assemblyName, string typeName, object[] activationAttributes);
        object CreateInstanceAndUnwrap(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes);
    }
}
