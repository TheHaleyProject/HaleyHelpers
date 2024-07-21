﻿using Haley.Enums;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Haley.Models {
    public class StorageResponseBase {
        public bool Status { get; set; }
        public string Message { get; set; }
        public StorageResponseBase() {  }
    }
}
