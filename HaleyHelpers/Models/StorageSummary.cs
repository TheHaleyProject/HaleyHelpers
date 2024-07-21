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
    public class StorageSummary {
        public bool Status { get; set; }
        //public string BasePath { get; set; } //Don't expose the base path to external result
        public string TargetName { get; set; }
        public string RawName { get; set; }
        public string Message { get; set; }
        protected bool IsRepository { get; set; } = true;
        public StorageSummary() {  }
    }
}
