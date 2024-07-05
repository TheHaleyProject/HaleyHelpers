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
        public string ParentDirectoryName { get; set; } = "Data";
        public string FileExtension { get; set; }
        public Stream FileStream { get; set; }
        public bool PreferId { get; set; } = true;
        public int DirectoryDepth { get; internal set; } = 5;
        public string Hash { get; private set; }
        public string TargetFileName { get; private set; }
        public string TargetDirectory { get; private set; }
        public FileExistsResolveMode ResolveMode { get; set; } = FileExistsResolveMode.Throw;

        public StorageInput Process() {
            if (PreferId && Id < 1) throw new ArgumentException($@"Invalid Id value : {Id}");
            if (!PreferId && string.IsNullOrWhiteSpace(FileName)) throw new ArgumentException($@"Invalid FileName : Non-null value is needed if PreferID property is set to false");
            var target = PreferId ? Id.ToString() : FileName;
            //if (FileStream == null) throw new ArgumentException($@"File stream for {target} is null. Nothing to save.");

            //if basepath is empty, set a base path as Data
            if (string.IsNullOrWhiteSpace(ParentDirectoryName)) ParentDirectoryName = "Data";
            if (string.IsNullOrWhiteSpace(FileExtension) && !PreferId) {
                //We have a possibility to check the file extension from the file name , in case it is available
                var ext = Path.GetExtension(FileName);
                if (!string.IsNullOrWhiteSpace(ext)) {
                    FileExtension = ext;
                }
            }
            List<string> dirBuilder = new List<string>();
            if (PreferId) {
                if (Id < 100) {
                    //meaning it has to go into 00 folder.
                    dirBuilder.Add("00");
                }
                var idString = Id.ToString();

            }else {
                Hash = HashUtils.ComputeHash(Path.GetFileNameWithoutExtension(FileName).ToLowerInvariant(), HashMethod.MD5, false);
                Hash = Hash.ToLowerInvariant();
            }
            return this;
        }
        public StorageInput() { }
    }
}
