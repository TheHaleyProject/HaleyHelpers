using Haley.Abstractions;
using System.Collections.Generic;

namespace Haley.Models {
    public class OSSReadRequest : IOSSRead {
        public string TargetPath { get; set; }
        public string TargetName { get; set; }
        public OSSCtrld Client { get; set; } = new OSSCtrld();
        public OSSCtrld Module { get; set; } = new OSSCtrld();
        public int Version { get; set; } = 0; //Send latest
        public List<OSSRoute> StorageRoutes { get; } = new List<OSSRoute>(); //Initialization. We can only then clear, or Add.
        public virtual OSSReadRequest SetClient(OSSCtrld input) {
            Client = input;
            return this;
        }
        public virtual OSSReadRequest SetModule(OSSCtrld input) {
            Module = input;
            return this;
        }
        public OSSReadRequest() {
        }
    }
}
