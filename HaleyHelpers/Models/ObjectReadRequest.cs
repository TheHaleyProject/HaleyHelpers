using Haley.Abstractions;
using System.Collections.Generic;

namespace Haley.Models {
    public class ObjectReadRequest : IObjectReadRequest {
        public string ObjectLocation { get; set; }
        public List<StorageRoute> StorageRoutes { get; } = new List<StorageRoute>(); //Initialization. We can only then clear, or Add.
        public ObjectReadRequest() { }
    }
}
