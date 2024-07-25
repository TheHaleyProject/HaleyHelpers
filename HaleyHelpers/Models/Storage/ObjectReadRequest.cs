using Haley.Abstractions;
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
    public class ObjectReadRequest : IObjectReadRequest {
        public string ObjectLocation { get; set; }

        public List<StorageRoute> StorageRoutes { get; } = new List<StorageRoute>(); //Initialization. We can only then clear, or Add.

        public ObjectReadRequest() { }
    }
}
