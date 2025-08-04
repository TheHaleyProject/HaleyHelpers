using Haley.Abstractions;
using System.Collections.Generic;

namespace Haley.Models {
    public class OSSReadRequest : IOSSRead {
      
        public string TargetPath { get; set; }
        public string TargetName { get; set; }
        public OSSName Client { get; set; } = new OSSName();
        public OSSName Module { get; set; } = new OSSName();
        public List<OSSRoute> StorageRoutes { get; } = new List<OSSRoute>(); //Initialization. We can only then clear, or Add.
        public virtual OSSReadRequest SetClient(OSSName input) {
            Client = input;
            return this;
        }
        public virtual OSSReadRequest SetModule(OSSName input) {
            Module = input;
            return this;
        }
        public OSSReadRequest() {
        }
    }
}
