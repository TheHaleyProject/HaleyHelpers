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
    public class RepoStorageRequest: RepoStorageRequestBase  {
        //public bool IsFolder { get; set; }
        public StorageFileConflict ResolveMode { get; set; } = StorageFileConflict.ReturnError;
        public RepoStorageRequest() { 
            //IsFolder = false; 
        }
    }
}
