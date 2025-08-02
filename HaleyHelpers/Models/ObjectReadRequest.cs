using Haley.Abstractions;
using System.Collections.Generic;

namespace Haley.Models {
    public class ObjectReadRequest : IObjectReadRequest {
        public const string DEFAULTNAME = "_dssdefault";
        public string TargetPath { get; set; }
        public StorageNameInfo Client { get; set; }
        public StorageNameInfo Module { get; set; }
        public List<StorageRoute> StorageRoutes { get; } = new List<StorageRoute>(); //Initialization. We can only then clear, or Add.
        public virtual ObjectReadRequest SetClient(string name, bool isControlled = true) {
            Client = new StorageNameInfo(name, isControlled);
            return this;
        }
        public virtual ObjectReadRequest SetModule(string name, bool isControlled = true) {
            Module = new StorageNameInfo(name, isControlled);
            return this;
        }
        public ObjectReadRequest() {
            SetClient(DEFAULTNAME, false);
            SetModule(DEFAULTNAME, false);
        }
    }
}
