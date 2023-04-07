using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Haley.Models;

namespace Haley.Utils
{
    public static class EncryptionExtensions
    {
        public static IEnumerable<K2Sequence> ChangeDirection(this IEnumerable<K2Sequence> input,bool reverseList = false)
        {
            var output_sequence = new List<K2Sequence>(input); //Initiating new sequence list
            output_sequence.ForEach(p => p.ChangeDirection());
            if (reverseList) output_sequence.Reverse(); //Reverse the list itself.
            return output_sequence;
        }
    }
}
