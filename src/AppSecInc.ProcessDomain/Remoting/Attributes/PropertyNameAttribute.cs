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
using System.Text;

namespace AppSecInc.ProcessDomain.Remoting.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PropertyNameAttribute : Attribute
    {
        public PropertyNameAttribute(string name)
        {
            Name = name;
        }

        public PropertyNameAttribute()
        {
        }

        public string Name { get; set; }
    }
}
