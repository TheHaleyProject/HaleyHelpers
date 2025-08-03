using System.IO;
using Haley.Abstractions;

namespace Haley.Models {
    public class FileStreamResponse : Feedback,IOSSFileStreamResponse {
        public Stream Stream { get; set; }
        public string Extension { get; set; }
        public FileStreamResponse() {  }
    }
}
