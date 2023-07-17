using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;
using Haley.Internal;

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
        /// Get Big Integer
        /// </summary>
        /// <param name="time">Date time</param>
        /// <param name="length">max length should be 17. min length is 8</param>
        /// <returns></returns>
        public static long GetBigInt(DateTime time, int length = 9) {

            //Maximum for a 64 bit long is 2^ 64 -1 (18446744073709551615) 20 digit. So, a safer side is 19 digit
            //limit at 15 characters (we get a minimum of 10 to a maximum of 15 characters)

            if (length < 9) length = 9;
            if (length > 17) length = 17;

            //From below two we get a 16-17 characters.
            var timecomp = GetTimeComponent(time).ToString(); //
            var randomvalue = ConcatRandomNumbers(5); //Gives 10 to 15 possible digits
            var result = timecomp + randomvalue;
            if (result.Length < length) {
                return long.Parse(result.PadRight(length, zero)); //If length is less, pad
            } else {
                return long.Parse(result.Substring(0, length));
            }
        }

        /// <summary>
        /// Will give a random time based number
        /// </summary>
        /// <param name="length">Max length should be 17</param>
        /// <returns></returns>
        public static long GetBigInt(int length = 9) {
            return GetBigInt(DateTime.UtcNow, length);
        }

        public static long GetTimeComponent(DateTime time, int hour_divider = 3) {

            if (hour_divider < 3) hour_divider = 3; //We get the hours completed between start and end date and divide the value by divider.
            if (hour_divider > 96) hour_divider = 96;

            //Add all components.
            var ts = time - Constants.B_EPOCH;
            return Convert.ToInt64(ts.TotalHours / hour_divider);
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