using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Xml.Linq;

namespace Haley.Models {
    public class OSSControlled :OSSInfo , IOSSControlled{
        public string SaveAsName { get; set; } //Should be the controlled name or a name compatible for the database 
        public OSSControlMode ControlMode { get; set; } //Parsing or create mode is defined at application level?
        public OSSParseMode ParseMode { get; set; } //If false, we fall back to parsing.
        public OSSControlled(string displayname = DEFAULTNAME, OSSControlMode control = OSSControlMode.None, OSSParseMode parse = OSSParseMode.Parse) :base(displayname) {
            ControlMode = control;
            ParseMode = parse;
        }
    }
}
