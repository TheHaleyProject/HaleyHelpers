using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public class DSSConfig : IDSSConfig{
        //All suffix are applicable only when dealing with controlled names.
        public string SuffixClient { get; set; } = "c";
        public string SuffixModule { get; set; } = "m";
        public string SuffixWorkSpace { get; set; } = "w";
        public string SuffixFile { get; set; } = "f";
        public int SplitLengthNumber { get; set; } = 2; //For numbers
        public int DepthNumber { get; set; } = 0;
        public int SplitLengthHash { get; set; } = 1; //For Hash
        public int DepthHash { get; set; } = 8;
    }
}
