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
    public class StorageRequest {
        public object Id { get; set; } //Could be number or string | This is supposedly the parameter key which user provides while making the request.
        public string RawName { get; set; } //Could be a file name (with extension) or a folder name | This will be returned back to the user.
        public string StoredName { get; set; }
        public bool IsFolder { get; set; } = false;
        public string Extension { get; protected set; }
        public bool PreferNumericName { get; set; } = true;
        public string RootDir { get; set; }
        public FileExistsResolveMode ResolveMode { get; set; } = FileExistsResolveMode.ReturnError;
        public StorageRequest() { }
    }
}
