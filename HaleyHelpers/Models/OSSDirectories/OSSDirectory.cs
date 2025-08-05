using Haley.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public class OSSDirectory : OSSCtrld , IOSSDirectory{
        public string Path { get; set; }
    }
}
