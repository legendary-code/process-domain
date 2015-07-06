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

namespace AppSecInc.ProcessDomain
{
    public enum PlatformTarget
    {
        AnyCPU,
        x86,
        Itanium,
        x64
    }

    public static class PlatformTargetUtil
    {
        public static string GetCompilerArgument(PlatformTarget target)
        {
            switch (target)
            {
                case PlatformTarget.AnyCPU:
                    return "/platform:anycpu";
                case PlatformTarget.Itanium:
                    return "/platform:Itanium";
                case PlatformTarget.x64:
                    return "/platform:x64";
                case PlatformTarget.x86:
                    return "/platform:x86";
            }

            throw new NotSupportedException("Unknown platform target specified");
        }
    }
}
