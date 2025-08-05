using Haley.Abstractions;
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
    public class OSSWriteRequest : OSSReadRequest, IOSSWrite {
        public string FileOriginalName { get; set; } //actual file name.
        public OSSResolveMode ResolveMode { get; set; } = OSSResolveMode.ReturnError;
        public int BufferSize { get; set; } = 8192;
        public string Id { get; set; }
        public Stream FileStream { get; set; }

        public new OSSWriteRequest SetClient(OSSCtrld input) {
             base.SetClient(input);
            return this;
        }

        public new OSSWriteRequest SetModule(OSSCtrld input) {
            base.SetModule(input);
            return this;
        }
        public OSSWriteRequest() { }

        public virtual object Clone() {
            var cloned = new OSSWriteRequest();
            //use map
            this.MapProperties(cloned);
            return cloned ;
        }
    }
}
