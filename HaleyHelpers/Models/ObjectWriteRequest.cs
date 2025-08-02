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
    public class ObjectWriteRequest : ObjectReadRequest, IObjectUploadRequest {
        public string RawName { get; set; }
        public ObjectExistsResolveMode ResolveMode { get; set; } = ObjectExistsResolveMode.ReturnError;
        public int BufferSize { get; set; } = 8192;
        public string Id { get; set; }
        public Stream FileStream { get; set; }

        public new ObjectWriteRequest SetClient(string name, bool isControlled = true) {
             base.SetClient(name, isControlled);
            return this;
        }

        public new ObjectWriteRequest SetModule(string name, bool isControlled = true) {
            base.SetModule(name, isControlled);
            return this;
        }
        public ObjectWriteRequest() { }

        public virtual object Clone() {
            var cloned = new ObjectWriteRequest();
            //use map
            this.MapProperties(cloned);
            return cloned ;
        }
    }
}
