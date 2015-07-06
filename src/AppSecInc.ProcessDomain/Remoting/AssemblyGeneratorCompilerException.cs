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
using System.CodeDom.Compiler;
using System.Runtime.Serialization;
using System.Text;

namespace AppSecInc.ProcessDomain.Remoting
{
    [Serializable]
    public class AssemblyGeneratorCompilerException : Exception
    {
        public AssemblyGeneratorCompilerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AssemblyGeneratorCompilerException(string message, CompilerErrorCollection errors)
            : base(message)
        {
            Errors = errors;
        }

        public CompilerErrorCollection Errors
        {
            get;
            private set;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Message);

            foreach (CompilerError error in Errors)
            {
                if (error.IsWarning)
                    continue;
                sb.AppendFormat("{0}\r\n", error);
            }

            return sb.ToString();
        }
    }
}
