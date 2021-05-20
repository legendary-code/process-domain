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
