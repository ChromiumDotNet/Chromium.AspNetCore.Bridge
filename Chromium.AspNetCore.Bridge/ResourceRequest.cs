// Copyright (c) Alex Maitland. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace Chromium.AspNetCore.Bridge
{
    /// <summary>
    /// Resource Request - Represents basic request information, Url, Method (GET/POST/
    /// </summary>
    public class ResourceRequest
    {
        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; private set; }
        /// <summary>
        /// Http Request Method
        /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods
        /// </summary>
        public string Method { get; private set; }
        /// <summary>
        /// Dictionary of Http Headers
        /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers
        /// </summary>
        public IDictionary<string, string[]> Headers { get; private set; }
        /// <summary>
        /// Request Body
        /// (Optional)
        /// </summary>
        public Stream Stream { get; private set; }

        /// <summary>
        /// Resource Request
        /// </summary>
        /// <param name="url"><see cref="Url"/></param>
        /// <param name="method"><see cref="Method"/></param>
        /// <param name="headers"><see cref="Headers"/></param>
        /// <param name="stream"><see cref="Stream"/></param>
        public ResourceRequest(string url, string method, IDictionary<string, string[]> headers, Stream stream = null)
        {
            Url = url;
            Method = method;
            Headers = headers;
            Stream = stream;
        }

        /// <summary>
        /// Resource Request
        /// </summary>
        /// <param name="url"><see cref="Url"/></param>
        /// <param name="method"><see cref="Method"/></param>
        /// <param name="headers">will be converted to<see cref="Headers"/></param>
        /// <param name="stream"><see cref="Stream"/></param>
        public ResourceRequest(string url, string method, NameValueCollection headers, Stream stream = null) : this(url, method, ToDictionary(headers), stream)
        {
            
        }

        /// <summary>
        /// Resource Request
        /// </summary>
        /// <param name="url"><see cref="Url"/></param>
        /// <param name="method"><see cref="Method"/></param>
        /// <param name="headers">will be converted to <see cref="Headers"/></param>
        /// <param name="stream"><see cref="Stream"/></param>
        public ResourceRequest(string url, string method, IEnumerable<KeyValuePair<string, string>> headers, Stream stream = null) : this(url, method, ToDictionary(headers), stream)
        {

        }

        private static IDictionary<string, string[]> ToDictionary(IEnumerable<KeyValuePair<string, string>> headers)
        {
            var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                dict.Add(header.Key, new string[] { header.Value });
            }
            return dict;
        }

        private static IDictionary<string, string[]> ToDictionary(NameValueCollection nameValueCollection)
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
