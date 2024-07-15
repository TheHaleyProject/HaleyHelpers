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
    public class StorageOutput {
        public int StoredCount { get; set; }
        public int FailedCount { get; set; }
        public string TotalSizeUploaded { get; set; }
        public Dictionary<string, FileSaveSummary> StoredFilesInfo { get; set; } = new Dictionary<string, FileSaveSummary>();
        public Dictionary<string, FileSaveSummary> FailedFilesInfo { get; set; } = new Dictionary<string, FileSaveSummary>();
        public StorageOutput() {  }
    }
}
