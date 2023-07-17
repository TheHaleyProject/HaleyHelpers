using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;

namespace Haley.Utils
{
    public static class RandomUtils
    {
        static Random rand = new Random();
        static Random rand2 = new Random();
        static char zero = '0';  // zero will return null

        public static bool GetBool()
        {
            if (rand.Next(0, 2) == 0) //Number will be betweewn 0 and 1.
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static int GetZeroOne()
        {
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
        public static long GetBigInt(DateTime time, int length = 8) {

            //Maximum for a 64 bit long is 2^ 64 -1 (18446744073709551615) 20 digit. So, a safer side is 19 digit
            //limit at 15 characters (we get a minimum of 10 to a maximum of 15 characters)

            if (length < 8) length = 8;
            if (length > 17) length = 17;

            //From below two we get a 16-17 characters.
            var timecomp = GetTimeComponent(time).ToString(); // min 4 / max 5 digits 
            var randomvalue = (GetNumber().ToString() + GetNumber().ToString() + GetNumber().ToString() + GetNumber().ToString()); //12 digits
            var result = timecomp + randomvalue;
            if (result.Length < length) {
                return long.Parse(result.PadRight(length, zero)); //If length is less, pad
            } else { 
                return long.Parse(result.Substring(0,length));
            }
        }

        /// <summary>
        /// Will give a random time based number with 15 digits (first 4 or 5 digit corresponds to the UTC time) rest are random.
        /// </summary>
        /// <param name="length">Max length should be 17</param>
        /// <returns></returns>
        public static long GetBigInt(int length = 8) {
            return GetBigInt(DateTime.UtcNow,length); 
        }

        static long GetTimeComponent(DateTime time) {
            //Maximum possibility 5 digits (until the year 9999.. probably humanity won't be alive until then..)
            //Add all components.
            var addedTime = time.Year + time.Month + time.Day + time.Hour + time.Minute + time.Second + time.Millisecond;
            return addedTime; 
        }

        static long GetNumber() {
            return rand2.Next(111, 999); //3 digit random
        }
    }
}