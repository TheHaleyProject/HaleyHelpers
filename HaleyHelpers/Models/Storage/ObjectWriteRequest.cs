﻿using Haley.Abstractions;
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
    public class ObjectWriteRequest : ObjectReadRequest, IObjectUploadRequest {
        public string ObjectRawName { get; set; }
        public ObjectExistsResolveMode ResolveMode { get; set; } = ObjectExistsResolveMode.ReturnError;
        public int BufferSize { get; set; } = 8192;
        public Stream FileStream { get; set; }
        public string ObjectId { get; set; }
        public ObjectWriteRequest() { }
    }
}