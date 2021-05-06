// Copyright (c) Alex Maitland. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Chromium.AspNetCore.Bridge
{
    public class ResourceResponse
    {
        /// <summary>
        /// The HTTP response status code.
        /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Status
        /// </summary>
        public int StatusCode { get; private set; }
        /// <summary>
        /// The HTTP response reason phrase.
        /// </summary>
        public string ReasonPhrase { get; private set; }
        /// <summary>
        /// The HTTP Response headers
        /// https://developer.mozilla.org/en-US/docs/Glossary/Response_header
        /// </summary>
        public IDictionary<string, string[]> Headers { get; private set; }
        
        /// <summary>
        /// Response Body
        /// </summary>
        public Stream Stream { get; private set; }

        /// <summary>
        /// ResourceResponse 
        /// </summary>
        /// <param name="statusCode"><see cref="StatusCode"/></param>
        /// <param name="reasonPhrase"><see cref="ReasonPhrase"/></param>
        /// <param name="headers"><see cref="Headers"/></param>
        /// <param name="stream"><see cref="Stream"/></param>
        public ResourceResponse(int statusCode, string reasonPhrase, IDictionary<string, string[]> headers, Stream stream)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Headers = headers;
            Stream = stream;
        }

        /// <summary>
        /// Gets a raw response header string delimited by newline.
        /// </summary>
        /// <returns>headers as a string</returns>
        public string GetHeaderString()
        {
            //TODO: Investigate performance improvements 
            var responseHeaders = new StringBuilder();

            //Add the response headers from OWIN to the string
            foreach (var header in Headers)
            {
                //It's possible for headers to have multiple values
                foreach (var val in header.Value)
                {
                    responseHeaders.AppendLine(header.Key + ":" + val);
                }
            }

            return responseHeaders.ToString();
        }
    }
}
