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
    public class ObjectCreateSummary :Feedback {
        public int Passed { get; set; }
        public int Failed { get; set; }
        public string TotalSizeUploaded { get; set; }
        public List<ObjectCreateResponse> PassedObjects { get; set; } = new List<ObjectCreateResponse>();
        public List<ObjectCreateResponse> FailedObjects { get; set; } = new List<ObjectCreateResponse>();
        public ObjectCreateSummary() {  }
    }
}
