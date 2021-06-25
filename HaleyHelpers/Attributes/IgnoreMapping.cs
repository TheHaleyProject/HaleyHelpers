using System;
using Haley.Utils;

namespace Haley.Models
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreMappingAttribute : Attribute
    {
        public IgnoreMappingMode Mode { get; set; }
        public IgnoreMappingAttribute() { }
        public IgnoreMappingAttribute(IgnoreMappingMode mode) { Mode = mode; }
    }
}
