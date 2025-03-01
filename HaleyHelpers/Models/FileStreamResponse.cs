using System.IO;
using Haley.Abstractions;

namespace Haley.Models {
    public class FileStreamResponse : Feedback,IFileStreamResponse {
        public Stream Stream { get; set; }
        public string Extension { get; set; }
        public FileStreamResponse() {  }
    }
}
