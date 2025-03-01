using Haley.Abstractions;
using System.Collections.Generic;

namespace Haley.Models {
    public class ObjectCreateSummary :Feedback, IObjectCreateSummary {
        public int Passed { get; set; }
        public int Failed { get; set; }
        public string TotalSizeUploaded { get; set; }
        public List<IObjectCreateResponse> PassedObjects { get; set; } = new List<IObjectCreateResponse>();
        public List<IObjectCreateResponse> FailedObjects { get; set; } = new List<IObjectCreateResponse>();
        public ObjectCreateSummary() {  }
    }
}
