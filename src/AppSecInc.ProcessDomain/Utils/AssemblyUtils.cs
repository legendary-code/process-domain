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

namespace AppSecInc.ProcessDomain.Utils
{
    public static class AssemblyUtils
    {
        public static string GetFilePathFromFileUri(string uri)
        {
            var fileUri = new Uri(uri);
            return Uri.UnescapeDataString(fileUri.AbsolutePath).Replace('/', '\\');
        }
    }
}