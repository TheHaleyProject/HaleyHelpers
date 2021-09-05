using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Haley.Models
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OtherNamesAttribute : Attribute
    {
        public List<string> AlternativeNames { get; private set; }
        public OtherNamesAttribute() { AlternativeNames = new List<string>(); }
        public OtherNamesAttribute(params string[] names) { AlternativeNames = names.ToList(); }
    }
}
