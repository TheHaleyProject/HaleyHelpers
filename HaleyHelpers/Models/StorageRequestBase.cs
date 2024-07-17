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
    public class StorageRequestBase {
        public string TargetName { get; set; } //To be filled for read requests.. 
        public string RootDir { get; set; }
        public bool IsFolder { get; set; } = false;
        public StorageRequestBase() { }
    }
}
