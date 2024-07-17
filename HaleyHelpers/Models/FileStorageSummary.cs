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
    public sealed class FileStorageSummary : StorageSummary {
        public long Size { get; set; }
        public string Extension { get; set; }
        public FileStorageSummary() { IsDirectory = false;  }
    }
}
