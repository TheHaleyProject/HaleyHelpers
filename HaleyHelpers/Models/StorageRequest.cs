using Haley.Enums;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Haley.Models {
    public class StorageRequest:StorageRequestBase {
        public string Id { get; set; } //Could be number or string | This is supposedly the parameter key which user provides while making the request.
        public string RawName { get; set; } //Could be a file name (with extension) or a folder name | This will be returned back to the user.
        public bool ForcedHash { get; set; } = false;
        public FileNamePreference Preference { get; set; } = FileNamePreference.Number;
        public FileNameSource Source { get; set; } = FileNameSource.Id;
        public FileExistsResolveMode ResolveMode { get; set; } = FileExistsResolveMode.ReturnError;
        public StorageRequest() { }
    }
}
