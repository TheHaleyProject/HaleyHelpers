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
    public class StorageResponse :StorageResponseBase {
        public int Passed { get; set; }
        public int Failed { get; set; }
        public string TotalSizeUploaded { get; set; }
        public Dictionary<string, FileStorageSummary> PassedSummary { get; set; } = new Dictionary<string, FileStorageSummary>();
        public Dictionary<string, FileStorageSummary> FailedSummary { get; set; } = new Dictionary<string, FileStorageSummary>();
        public StorageResponse() {  }
    }
}
