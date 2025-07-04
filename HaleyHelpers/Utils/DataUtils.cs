using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Haley.Utils
{
    public static class DataUtils
    {
        public static async Task<IFeedback> PagedDataFetch<T>(List<T> source, Func<int, int, Task<List<T>>> dataprovider,
            int count = 100) {
            int skip = 0;

            while (true) {
                List<T> datalist;
                try {
                    datalist = await dataprovider(skip, count);
                } catch (Exception ex) {
                    return new Feedback(false, $"Fetch failed at skip={skip}: {ex.Message}");
                }
                if (datalist == null || datalist.Count == 0) break;
                source.AddRange(datalist);
                if (datalist.Count < count) break; //If the count is less than requested, we assume there are no more items to fetch.
                skip += count;
            }
            return new Feedback(true, $"Fetched {source.Count} items successfully.");
        }

        public static IEnumerable<object> RecursiveGroup<T,K>(IEnumerable<T> input, List<K> keys, Func<T,K,object> groupMaker, Func<object,IEnumerable<object>, object>resultMaker, int level = 0) {
            if (level >= keys.Count || input.Count() == 0) return Enumerable.Empty<object>();
            var currentKey = keys[level];
            return input
                .GroupBy(p => groupMaker(p,currentKey))
                .Select(gp => {
                    var children = (level + 1 < keys.Count)
                        ? RecursiveGroup(gp.ToList(), keys, groupMaker,resultMaker, level + 1)
                        : null;
                    // You can customize the output shape here
                    return  resultMaker(gp.Key, children ?? Enumerable.Empty<object>());
                });
        }

        public static IEnumerable<>

    }
}
