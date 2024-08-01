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
    public class DirectoryInfoResponse : Feedback {
        public string Path { get; set; }
        public List<string> FoldersList { get; set; } = new List<string>();
        public List<string> FilesList { get; set; } = new List<string>();
        public DirectoryInfoResponse() {  }
    }
}
