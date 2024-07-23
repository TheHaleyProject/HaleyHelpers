using System;
using Haley.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;
using System.IO;

namespace Haley.Abstractions {
    public interface IObjectUploadRequest : IObjectReadRequest {
        int BufferSize { get; set; }
        Stream  FileStream { get; set; }
        string ObjectRawName { get; set; }
        string ObjectId { get; set; }
    }
}
