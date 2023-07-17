using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;
using Haley.Internal;
using Haley.Enums;

namespace Haley.Utils
{
    public static class RandomUtils {
        static Random rand = new Random();
        static Random rand2 = new Random();
        static char zero = '0';  // zero will return null

        public static bool GetBool() {
            if (rand.Next(0, 2) == 0) //Number will be betweewn 0 and 1.
            {
                return true;
            } else {
                return false;
            }
        }
        public static int GetZeroOne() {
            return rand.Next(0, 2);
        }

        public static string GetString(int number_of_bits = 1024) {
            return Convert.ToBase64String(HashUtils.GetRandomBytes(number_of_bits).bytes);
        }

        /// <summary>
        /// Will give a random time based number
        /// </summary>
        /// <param name="length">Max length should be 17</param>
        /// <returns></returns>
        public static long GetBigInt(int length = 9, TimeComp comp = TimeComp.Hour, int divider = 3) {
            return GetBigInt(DateTime.UtcNow, length, comp);
        }

        /// <summary>
        /// Will give a random time based number
        /// </summary>
        /// <param name="length">Max length should be 17</param>
        /// <returns></returns>
        public static long GetBigInt(int length = 9) {
            return GetBigInt(length);
        }

        /// <summary>
        /// Get Big Integer
        /// </summary>
        /// <param name="time">Date time</param>
        /// <param name="length">max length should be 17. min length is 8</param>
        /// <returns></returns>
        public static long GetBigInt(DateTime time, int length = 9) {

            return GetBigInt(time, length);
        }

        /// <summary>
        /// Get Big Integer
        /// </summary>
        /// <param name="time">Date time</param>
        /// <param name="length">max length should be 17. min length is 8</param>
        /// <returns></returns>
        public static long GetBigInt(DateTime time, int length, TimeComp comp, int comp_divider = 3) {

            //Maximum for a 64 bit long is 2^ 64 -1 (18446744073709551615) 20 digit. So, a safer side is 19 digit
            //limit at 15 characters (we get a minimum of 10 to a maximum of 15 characters)

            if (length < 9) {
                //For hour, we cannot go below 9
                if (comp == TimeComp.Day) {
                    if (length < 5) length = 5;
                } else {
                    length = 9;
                }
            }
            if (length > 17) length = 17;

            //From below two we get a 16-17 characters.
            var timecomp = GetTimeComponent(time,comp, comp_divider).ToString(); //
            var randomvalue = ConcatRandomNumbers(5); //Gives 10 to 15 possible digits
            var result = timecomp + randomvalue;
            if (result.Length < length) {
                return long.Parse(result.PadRight(length, zero)); //If length is less, pad
            } else {
                return long.Parse(result.Substring(0, length));
            }
        }

        public static long GetTimeComponent(DateTime time, TimeComp comp = TimeComp.Hour, int comp_divider = 3) {

            if (comp_divider < 3) comp_divider = 3; 
            if (comp_divider > 36) comp_divider = 36;

            //Add all components.
            var ts = time - Constants.B_EPOCH;

            if (comp == TimeComp.Day) {
                return Convert.ToInt64(ts.TotalDays / comp_divider);
            }
            return Convert.ToInt64(ts.TotalHours / comp_divider);
        }

        static long GetRandomNumber() {
            return rand2.Next(10, 999); //2-3 digit random
        }

        static string ConcatRandomNumbers(int count =3) {
            //3 counts will give 6 to 9 possibilities
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++) {
                sb.Append(GetRandomNumber().ToString());
            }
            return sb.ToString();
        }
    }
}