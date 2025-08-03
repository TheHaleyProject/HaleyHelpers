using Haley.Abstractions;
using System.Collections.Generic;

namespace Haley.Models {
    public class DirectoryInfoResponse : Feedback, IOSSDirResponse  {
        public string Path { get; set; }
        public List<string> FoldersList { get; set; } = new List<string>();
        public List<string> FilesList { get; set; } = new List<string>();
        public DirectoryInfoResponse() {  }
    }
}
