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
        public StorageNameHashMode HashMode { get; set; } = StorageNameHashMode.ParseOrCreate;
        public StorageNamePreference Preference { get; set; } = StorageNamePreference.Number;
        public StorageNameSource Source { get; set; } = StorageNameSource.Id;
        public StorageFileConflict ResolveMode { get; set; } = StorageFileConflict.ReturnError;
        public StorageRequest() { }
    }
}
