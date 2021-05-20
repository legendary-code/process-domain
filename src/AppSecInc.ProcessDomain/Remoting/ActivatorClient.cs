using System;
using System.Collections;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace AppSecInc.ProcessDomain.Remoting
{
    /// <summary>
    /// This class provides access to an Activator in a remote process
    /// </summary>
    internal class ActivatorClient : IDisposable
    {
        readonly Activator _activator;
        readonly IChannel _channel;
        
        public ActivatorClient(string guid, ProcessDomainSetup setup)
        {
            var serverProvider = new BinaryServerFormatterSinkProvider { TypeFilterLevel = setup.TypeFilterLevel };
            var clientProvider = new BinaryClientFormatterSinkProvider();

            var properties = new Hashtable();
            properties["portName"] = string.Format(ActivatorHost.ClientChannelName, guid);
            properties["name"] = string.Format(ActivatorHost.ClientChannelName, guid);
            setup.Remoting.ApplyClientProperties(properties);

            _channel = new IpcChannel(properties, clientProvider, serverProvider);
            ChannelServices.RegisterChannel(_channel, false);

            _activator = (Activator)System.Activator.GetObject(typeof(Activator), string.Format("ipc://{0}/{1}", string.Format(ActivatorHost.ServerChannelName, guid), ActivatorHost.ActivatorName));
        }

        public Activator Activator
        {
            get { return _activator; }
        }

        public void Dispose()
        {
            ChannelServices.UnregisterChannel(_channel);
        }
    }
}
