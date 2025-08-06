using Haley.Abstractions;
using System.Collections.Generic;
using Haley.Enums;

namespace Haley.Models {
    public class OSSReadRequest : IOSSRead {
        public string TargetPath { get; set; }
        public string TargetName { get; set; }
        public IOSSControlled Client { get; set; } = new OSSControlled();
        public IOSSControlled Module { get; set; } = new OSSControlled();
        public IOSSControlled Workspace { get; set; } = new OSSControlled(control:OSSControlMode.Guid,parse:OSSParseMode.ParseOrGenerate);
        public int Version { get; set; } = 0; //Send latest
        public List<OSSRoute> StorageRoutes { get; } = new List<OSSRoute>(); //Initialization. We can only then clear, or Add.
        public virtual OSSReadRequest SetClient(OSSControlled input) {
            Client = input;
            return this;
        }
        public virtual OSSReadRequest SetModule(OSSControlled input) {
            Module = input;
            return this;
        }
        public OSSReadRequest() {
        }
    }
}
