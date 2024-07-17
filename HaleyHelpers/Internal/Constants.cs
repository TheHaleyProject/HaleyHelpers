using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Internal {
    internal class Constants {
        public static DateTime B_EPOCH = new DateTime(2014, 11, 13); //Assuming we start couting from this date. (Not from Epoch.
        public const int DIRDEPTH_LONG = 0; //Split till the end.
        public const int DIRDEPTH_HASH = 5; //Only 5 character is enough which itself might yield in millions
        public const int CHARSPLITLENGTH = 2;
    }
}
