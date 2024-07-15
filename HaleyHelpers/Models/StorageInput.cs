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
    public class StorageInput {
        public long Id { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; protected set; }
        public string StoredFileName { get; set; }
        public bool PreferId { get; set; } = true;
        public string RootDirName { get; set; } = "Data";
        public FileExistsResolveMode ResolveMode { get; set; } = FileExistsResolveMode.ReturnError;

        public virtual StorageInput Process() {
            //If we have the stored file name, then we are trying to read the data.. Disregard every other check.
            if (!string.IsNullOrWhiteSpace(StoredFileName)) return this;

            if (PreferId && Id < 1) throw new ArgumentException($@"Invalid Id value : {Id}");
            if (!PreferId && string.IsNullOrWhiteSpace(FileName)) throw new ArgumentException($@"Invalid FileName : Non-null value is needed if PreferID property is set to false");
          

            if (string.IsNullOrWhiteSpace(FileExtension) && !string.IsNullOrWhiteSpace(FileName)) {
                //We have a possibility to check the file extension from the file name , in case it is available
                var ext = Path.GetExtension(FileName);
                if (!string.IsNullOrWhiteSpace(ext)) {
                    FileExtension = ext;
                }
            }

            if (string.IsNullOrWhiteSpace(RootDirName)) RootDirName = "Data";

            return this;
        }
        public StorageInput() { }
    }
}
