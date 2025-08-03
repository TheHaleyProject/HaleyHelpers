using Haley.Abstractions;
using System.Collections.Generic;

namespace Haley.Models {
    public class ObjectCreateSummary :Feedback, IOSSSummary {
        public int Passed { get; set; }
        public int Failed { get; set; }
        public string TotalSizeUploaded { get; set; }
        public List<IOSSResponse> PassedObjects { get; set; } = new List<IOSSResponse>();
        public List<IOSSResponse> FailedObjects { get; set; } = new List<IOSSResponse>();
        public ObjectCreateSummary() {  }
    }
}
