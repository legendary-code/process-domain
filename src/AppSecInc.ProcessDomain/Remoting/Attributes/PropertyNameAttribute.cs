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
