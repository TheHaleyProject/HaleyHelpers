using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace Haley.Models {
    internal class DiskStorageRequest : StorageRequest {
        public string TargetPath { get; private set; } //path generated, except the BasePath from storageservice
        public void SetTargetPath(string path) {  TargetPath = path; } //TODO: Should allow to be set only once.
       
        public DiskStorageRequest() { }
        public DiskStorageRequest(StorageRequest input) { 
            input?.MapProperties(this); //map pr
        }
    }
}
