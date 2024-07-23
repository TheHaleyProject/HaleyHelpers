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
    public class ObjectCreateResponse {
        //Object can be a folder object or a file object.
        public bool Status { get; set; }
        //public string SavedName { get; set; } //We are not going to show this anymore.. not required for user to know
        public string RawName { get; set; }
        public string Message { get; set; }
        public long Size { get; set; }
        public bool ObjectExists { get; set; } = false;
        public ObjectCreateResponse() {  }
    }
}
