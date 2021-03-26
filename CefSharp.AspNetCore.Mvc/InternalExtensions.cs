using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace CefSharp.AspNetCore.Mvc
{
    internal static class InternalExtensions
    {
        public static IDictionary<string, string[]> ToDictionary(this NameValueCollection nameValueCollection)
        {
            var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in nameValueCollection.AllKeys)
            {
                if (!dict.ContainsKey(key))
                {
                    dict.Add(key, new string[0]);
                }
                var strings = nameValueCollection.GetValues(key);
                if (strings == null)
                {
                    continue;
                }
                foreach (string value in strings)
                {
                    var values = dict[key].ToList();
                    values.Add(value);
                    dict[key] = values.ToArray();
                }
            }
            return dict;
        }
    }
}