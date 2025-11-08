using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Microsoft.IdentityModel.Tokens.Experimental;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Haley.Utils
{
    public static class DataUtils
    {
        public static async Task<IFeedback> PagedDataFetch<T>(List<T> source, Func<int, int, Task<List<T>>> dataprovider,
            int batch_count = 100, int total_count = 0, int offset = 0) {
            int skip = offset; //Since this is initialized here, it is always zero.
            if (skip < 0) skip = 0; 
            var result = new List<T>();
            if (total_count < 0) total_count = 0;
            if (batch_count < 1) batch_count = 20; //Default batch count.

            while (true) {
                List<T> datalist;
                try {
                    datalist = await dataprovider(skip, batch_count);
                } catch (Exception ex) {
                    return new Feedback(false, $"Fetch failed at skip={skip}: {ex.Message}");
                }
                if (datalist == null || datalist.Count == 0) break;
                result.AddRange(datalist);
                if (datalist.Count <= batch_count) break; //If the count is less than requested, we assume there are no more items to fetch.
                if (total_count > 0 && result.Count >= total_count) break; //Sometimes, we might have skipped 1000 and are processing page 100, but still we haven't reached the total count, because we are not able to get the files.
                skip += batch_count;
            }
            source.AddRange(result);

            var payload = new JsonObject { ["skipped"] = skip, ["batch_count"] = batch_count, ["total_count"] = total_count}.ToJsonString();
            return new Feedback(true, $"Fetched {source.Count} items successfully.").SetResult(payload);
        }

        public static IEnumerable<object> RecursiveGroup<T,K>(IEnumerable<T> input, List<K> keys, Func<T, K, int, object> groupMaker, Func<object,object,int, object>resultMaker) {
            return RecursiveGroupInternal(input, keys, groupMaker, resultMaker, 0);
        }

        //TO DO : CHECK ABOUT CREATING TUPLES WITH REFLECTION.....
        //think about dynamic key selectors
        static IEnumerable<object> RecursiveGroupInternal<T, K>(IEnumerable<T> input, List<K> keys, Func<T, K,int, object> groupMaker, Func<object, object,int, object> resultMaker, int level = 0) {
            if (input == null) return Enumerable.Empty<object>();
            var materialized = input.ToList();
            if (level >= keys.Count || materialized.Count == 0) return Enumerable.Empty<object>();
            var currentKey = keys[level];
            return materialized //materialize
                .GroupBy(row => 
                groupMaker(row, currentKey,level))
                .Select(gp => {
                    var children = (level + 1 < keys.Count) //Should we go for one more hierarchical level??
                        ? RecursiveGroupInternal(gp, keys, groupMaker, resultMaker, level + 1)
                        : resultMaker(gp.Key, gp.ToList(), level+1); //If we reach the end, we still need to allow to process all the children
                    // You can customize the output shape here
                    return resultMaker(gp.Key, children ?? Enumerable.Empty<object>(),level);
                });
        }
    }
}