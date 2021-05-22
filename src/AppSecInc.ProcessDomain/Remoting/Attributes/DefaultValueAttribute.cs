using System;
using System.Collections.Generic;
using System.Text;

namespace AppSecInc.ProcessDomain.Remoting.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DefaultValueAttribute : Attribute
    {
        public DefaultValueAttribute(object value)
        {
            Value = value;
        }

        public DefaultValueAttribute()
        {
        }

        public object Value { get; set; }
    }
}
