using System;
using System.Collections.Generic;
using System.Text;
using Haley.Enums;
using Haley.Utils;

namespace Haley.Models
{
    [Serializable]
    public class K2Sequence
    {
        public string Id { get; set; }
        public long Key { get; set; }
        public K2Mode Method { get; }
        public bool IsReverse { get; private set; }

        public void ChangeDirection()
        {
            IsReverse = !IsReverse;
        }
        public K2Sequence(K2Mode method, bool is_reverse, long key)
        {
            Id = Guid.NewGuid().ToString();
            Method = method;
            IsReverse = is_reverse;
            if (key == 0) key = long.Parse("852654753");
            Key = key;
        }
        public K2Sequence(K2Mode method, bool is_reverse, string key)
        {
            Id = Guid.NewGuid().ToString();
            Method = method; 
            IsReverse = is_reverse;
            long new_key = 0;
            if (!long.TryParse(key, out new_key))
            {
                try
                {
                    var number = key.ToNumber();
                    int limit = 19;
                    if (number.Length < limit) limit = number.Length;
                    new_key = long.Parse(number.Substring(0, limit)); //Always take first 19 digits.
                }
                catch (Exception)
                {
                    new_key = long.Parse("654258357");
                }
            }
            Key = new_key;
        }
        public K2Sequence() { }
    }
}
