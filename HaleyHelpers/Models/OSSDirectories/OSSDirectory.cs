using Haley.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public abstract class OSSDirectory : OSSControlled , IOSSDirectory{
        public string Path { get; set; }
        public OSSDirectory(string displayName):base(displayName) { }
    }
}
