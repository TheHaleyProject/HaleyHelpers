using Haley.Enums;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Haley.Models {
    public class ObjectReadRequest {
        public string ObjectName { get; set; } //To be filled for read requests.. 
        public string ObjectFullPath { get; set; }
        public ObjectReadRequest() { }
    }
}
