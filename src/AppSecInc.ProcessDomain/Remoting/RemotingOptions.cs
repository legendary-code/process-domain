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
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using AppSecInc.ProcessDomain.Remoting.Attributes;

namespace AppSecInc.ProcessDomain.Remoting
{
    // Source: http://msdn.microsoft.com/en-us/library/bb187421(v=vs.80).aspx
    /// <summary>
    /// Various options for configuring remoting between parent and child processes. The client settings will apply to the parent process creating the process domain and the server settings will apply
    /// to the process domain process itself.
    /// </summary>
    [Serializable]
    public sealed class RemotingOptions
    {
        public RemotingOptions()
        {
            Server = new ServerOptions();
            Client = new ClientOptions();
            SetPropertiesToDefaultValues(Server);
            SetPropertiesToDefaultValues(Client);
        }

        [Serializable]
        public class GeneralOptions
        {
            internal GeneralOptions()
            {
                ExtraProperties = new Hashtable();
            }

            /// <summary>
            /// A Boolean value (true or false) that specifies whether communications on the channel are secure. The default is false. When it is set to true, the TokenImpersonationLevel property is set to Identification
            /// </summary>
            [PropertyName("secure")]
            [DefaultValue(false)]
            public bool Secure { get; set; }

            /// <summary>
            /// A value of type ProtectionLevel. The default is None, unless the secure property is set to true, in which case the default is EncryptAndSign. You must set the secure property to true to set the ProtectionLevel property to any value other than None. Note that None is the only setting that is compatible with Windows 95, Windows 98, or Windows Me.
            /// </summary>
            [PropertyName("protectionLevel")]
            [DefaultValue(ProtectionLevel.None)]
            public ProtectionLevel ProtectionLevel { get; set; }

            /// <summary>
            /// Any extra unimplemented properties to pass to IpcChannel
            /// </summary>
            public IDictionary ExtraProperties { get; private set; }
        }

        [Serializable]
        public sealed class ServerOptions : GeneralOptions
        {
            internal ServerOptions()
            {
            }

            /// <summary>
            /// A string that specifies the assembly, namespace, and class name of a class that implements the IAuthorizeRemotingConnection interface. The format of the string must be AuthorizationModuleNameSpace.AuthorizationModuleClass,AuthorizationModuleAssembly.
            /// </summary>
            [PropertyName("authorizationModule")]
            [DefaultValue(null)]
            public string AuthorizationModule { get; set; }

            /// <summary>
            /// A string that specifies the group or user that has permission to connect to this channel. The default is to allow access to all authorized users.
            /// </summary>
            [PropertyName("authorizedGroup")]
            [DefaultValue(null)]
            public string AuthorizedGroup { get; set; }

            /// <summary>
            /// A Boolean value (true or false) that specifies whether the server should impersonate the client. The default is false.
            /// </summary>
            [PropertyName("impersonate")]
            [DefaultValue(false)]
            public bool Impersonate { get; set; }
        }

        /// <summary>
        /// Remoting server options for the remote process domain process
        /// </summary>
        public ServerOptions Server { get; private set; }

        [Serializable]
        public sealed class ClientOptions : GeneralOptions
        {
            internal ClientOptions()
            {
            }

            /// <summary>
            /// A string that specifies the name that to be used as the connection group name on the server if the unsafeAuthenticatedConnectionSharing value is set to true. This property is ignored if unsafeAuthenticatedConnectionSharing is not set to true. If specified, make sure that this name maps to only one authenticated user. This property is supported only by version 1.1 or greater of the .NET Framework on the following platforms: Windows 98, Windows NT 4.0, Windows Me, Windows 2000, Windows XP Home Edition, Windows XP Professional, and the Windows Server 2003 family.
            /// </summary>
            [PropertyName("connectionGroupName")]
            [DefaultValue(null)]
            public string ConnectionGroupName { get; set; }

            /// <summary>
            /// An object that implements the ICredentials interface that represents the identity of the client.
            /// </summary>
            [PropertyName("credentials")]
            [DefaultValue(null)]
            public ICredentials Credentials { get; set; }

            /// <summary>
            /// A string that specifies a domain name to be used, in conjunction with the user name specified by username and the password specified by password, when authenticating to a server channel.
            /// </summary>
            [PropertyName("domain")]
            [DefaultValue(null)]
            public string Domain { get; set; }

            /// <summary>
            /// A string that specifies a password to be used, in conjunction with the user name specified by username and the domain specified by domain, when authenticating to a server channel.
            /// </summary>
            [PropertyName("password")]
            [DefaultValue(null)]
            public string Password { get; set; }


            /// <summary>
            /// A string that specifies the servicePrincipalName for Kerberos authentication. The default value is null.
            /// </summary>
            [PropertyName("serverPrincipalName")]
            [DefaultValue(null)]
            public string ServerPrincipalName { get; set; }

            /// <summary>
            /// A value of type TokenImpersonationLevel. This property specifies how the client is authenticated with the server. The default is None, unless the secure property is set to true, in which case the default is Identification.
            /// </summary>
            [PropertyName("tokenImpersonationLevel")]
            [DefaultValue(TokenImpersonationLevel.None)]
            public TokenImpersonationLevel TokenImpersonationLevel { get; set; }

            /// <summary>
            /// A Boolean value that indicates whether to allow high-speed NTLM-authenticated connection sharing. If this value is set to true, the connectionGroupName value must map to only one authenticated user. This property is ignored if the useAuthenticatedConnectionSharing value is set to true. This property is supported only by version 1.1 or greater of the .NET Framework on the following platforms: Windows 98, Windows NT 4.0, Windows Me, Windows 2000, Windows XP Home Edition, Windows XP Professional, and Windows Server 2003.
            /// </summary>
            [PropertyName("unsafeAuthenticatedConnectionSharing")]
            [DefaultValue(false)]
            public bool UnsafeAuthenticatedConnectionSharing { get; set; }

            /// <summary>
            /// A Boolean value that indicates whether the server channel reuses authenticated connections rather than authenticate each incoming call. By default, this value is set to true if the useDefaultCredentials value is also set to true; otherwise, the value is set to false, which means that each call is authenticated if the server requires authentication. This also applies to the programmatic equivalent, which is achieved either by creating an object that implements IDictionary, setting the credentials property to CredentialCache.DefaultCredentials, and passing that value to the channel sink, or by using the IDictionary returned from the ChannelServices.GetChannelSinkProperties method. This name/value pair is supported only by version 1.1 or greater of the .NET Framework on the following platforms: Microsoft Windows 98, Windows NT 4.0, Windows Millennium Edition (Windows Me), Windows 2000, Windows XP Home Edition, Windows XP Professional, and Windows Server 2003.
            /// </summary>
            [PropertyName("useAuthenticatedConnectionSharing")]
            [DefaultValue(true)]
            public bool UseAuthenticatedConnectionSharing { get; set; }

            /// <summary>
            /// A Boolean value that specifies whether to present credentials for the identity associated with the current thread when authenticating to a server channel.
            /// </summary>
            [PropertyName("useDefaultCredentials")]
            [DefaultValue(false)]
            public bool UseDefaultCredentials { get; set; }

            [PropertyName("username")]
            [DefaultValue(null)]
            public string Username { get; set; }
        }

        /// <summary>
        /// Remoting client options for the process creating and interacting with a process domain process
        /// </summary>
        public ClientOptions Client { get; private set; }

        internal void ApplyClientProperties(IDictionary properties)
        {
            Apply(Client, properties);
        }
        
        internal void ApplyServerProperties(IDictionary properties)
        {
            Apply(Server, properties);
        }

        private T GetAttribute<T>(PropertyInfo propInfo)
        {
            var attribs =  propInfo.GetCustomAttributes(typeof(T), false);
            if (attribs.Length == 0)
                return default(T);
            return (T)attribs[0];
        }

        private void SetPropertiesToDefaultValues(GeneralOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            foreach (var propInfo in options.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var propNameAttrib = GetAttribute<PropertyNameAttribute>(propInfo);
                var defaultValueAttrib = GetAttribute<DefaultValueAttribute>(propInfo);
                if (propNameAttrib == null || defaultValueAttrib == null)
                    continue;
                propInfo.SetValue(options, defaultValueAttrib.Value, null);
            }
        }
        
        private void Apply(GeneralOptions options, IDictionary properties)
        {
            if (options == null)
                throw new ArgumentNullException("options");
            if (properties == null)
                throw new ArgumentNullException("properties");

            foreach (var propInfo in options.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var propNameAttrib = GetAttribute<PropertyNameAttribute>(propInfo);
                var defaultValueAttrib = GetAttribute<DefaultValueAttribute>(propInfo);
                if (propNameAttrib == null || defaultValueAttrib == null)
                    continue;
                var value = propInfo.GetValue(options, null);               
                if ((value == null && defaultValueAttrib.Value == null) || (value != null && value.Equals(defaultValueAttrib.Value)))
                    continue;
                properties[propNameAttrib.Name] = value;
            }

            if (options.ExtraProperties != null)
            {
                foreach (var key in options.ExtraProperties.Keys)
                {
                    properties[key] = options.ExtraProperties[key];
                }
            }
        }
    }
}
