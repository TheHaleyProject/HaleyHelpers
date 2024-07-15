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
    public class FileSaveSummary {
        public long Size { get; set; }
        public bool Status { get; set; }
        public string BasePath { get; set; }
        public string StoredFileName { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public string Message { get; set; }
        public FileSaveSummary() {  }
    }
}
