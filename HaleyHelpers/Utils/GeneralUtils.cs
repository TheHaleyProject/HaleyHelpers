using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;

namespace Haley.Utils
{
    public static class GeneralUtils
    {
        static Random rand = new Random();
        static Random rand2 = new Random();
        static char zero = '0';  // zero will return null

        public static bool GetRandomBool()
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
        public static int GetRandomZeroOne()
        {
            return rand.Next(0, 2);
        }

        public static string GetRandomString(int number_of_bits = 1024) {
            return Convert.ToBase64String(HashUtils.GetRandomBytes(number_of_bits).bytes);
        }


        public static long GetRandomBigInt(DateTime time, bool padmissing = false) {
            //limit at 15 characters (we get a minimum of 10 to a maximum of 15 characters)

            var timecomp = GetTimeComponent(time).ToString(); // min 4 / max 5 digits 
            var randomvalue = (GetRandomNumber().ToString() + GetRandomNumber().ToString()); //min 6 / max 10 digits
            var result = timecomp + randomvalue;
            switch (padmissing) {
                case true:
                    return long.Parse(result.PadRight(15, zero));
                default:
                    return long.Parse(result);
            }
        }

        /// <summary>
        /// Will give a random time based number with 15 digits (first 4 or 5 digit corresponds to the UTC time) rest are random.
        /// </summary>
        /// <param name="padmissing">Will ensure that </param>
        /// <returns></returns>
        public static long GetRandomBigInt(bool padmissing = false) {
            return GetRandomBigInt(DateTime.UtcNow,padmissing); 
        }

        static long GetTimeComponent(DateTime time) {
            //Maximum possibility 5 digits (until the year 9999.. probably humanity won't be alive until then..)
            //Add all components.
            var addedTime = time.Year + time.Month + time.Day + time.Hour + time.Minute + time.Second + time.Millisecond;
            return addedTime; 
        }

        static long GetRandomNumber() {
            return rand2.Next(111, 99999); //Random 3-5 digit number
        }
    }
}