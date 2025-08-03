using Haley.Abstractions;
using System.Collections.Generic;

namespace Haley.Models {
    public class ObjectReadRequest : IOSSRead {
        public const string DEFAULTNAME = "default";
        public string TargetPath { get; set; }
        public string TargetName { get; set; }
        public OSSName Client { get; set; }
        public OSSName Module { get; set; }
        public List<OSSRoute> StorageRoutes { get; } = new List<OSSRoute>(); //Initialization. We can only then clear, or Add.
        public virtual ObjectReadRequest SetClient(string name, bool isControlled = true) {
            Client = new OSSName(name, isControlled);
            return this;
        }
        public virtual ObjectReadRequest SetModule(string name, bool isControlled = true) {
            Module = new OSSName(name, isControlled);
            return this;
        }
        public ObjectReadRequest() {
            SetClient(DEFAULTNAME, false);
            SetModule(DEFAULTNAME, false);
        }
    }
}
