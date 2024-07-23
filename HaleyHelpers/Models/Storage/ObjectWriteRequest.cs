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
    public class ObjectWriteRequest : ObjectReadRequest {
        public string RawName { get; set; }
        public ObjectExistsResolveMode ResolveMode { get; set; } = ObjectExistsResolveMode.ReturnError;
        public ObjectWriteRequest() { }
    }
}
