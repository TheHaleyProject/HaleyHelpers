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
    public class StorageResponse {
        public bool Status { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public string TotalSizeUploaded { get; set; }
        public Dictionary<string, StorageSummary> PassedSummary { get; set; } = new Dictionary<string, StorageSummary>();
        public Dictionary<string, StorageSummary> FailedSummary { get; set; } = new Dictionary<string, StorageSummary>();
        public StorageResponse() {  }
    }
}
