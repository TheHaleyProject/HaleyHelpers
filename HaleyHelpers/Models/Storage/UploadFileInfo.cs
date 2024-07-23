using System.IO;

namespace Haley.Models {
    public class UploadFileInfo {
        public string Id { get; set; }
        public string FileName { get; set; }
        public Stream FileStream { get; set; }
    }
}