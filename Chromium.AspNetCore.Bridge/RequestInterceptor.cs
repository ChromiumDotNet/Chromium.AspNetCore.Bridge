// Copyright (c) Alex Maitland. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Chromium.AspNetCore.Bridge
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public static class RequestInterceptor
    {
        /// <summary>
        /// Create the appropriate OWIN dictionary and process the request using the <paramref name="appFunc"/>
        /// </summary>
        /// <param name="appFunc">Owin App Function</param>
        /// <param name="request">Request information</param>
        /// <returns>Response</returns>
        //TODO: Implement cancellation token
        public static async Task<ResourceResponse> ProcessRequest(AppFunc appFunc, ResourceRequest request)
        {
            var requestStream = Stream.Null;
            var responseStream = new MemoryStream();

            if (request.Stream != null && request.Stream != Stream.Null)
            {
                //We need a seekable stream, we need to take a copy of CanSeek is false
                if (request.Stream.CanSeek)
                {
                    requestStream = request.Stream;
                }
                else
                {
                    requestStream = new MemoryStream();
                    request.Stream.CopyTo(requestStream);
                }
                requestStream.Position = 0;
            }

            var uri = new Uri(request.Url);

            //http://owin.org/html/owin.html#3-2-environment
            //The Environment dictionary stores information about the request,
            //the response, and any relevant server state.
            //The server is responsible for providing body streams and header collections for both the request and response in the initial call.
            //The application then populates the appropriate fields with response data, writes the response body, and returns when done.
            //Keys MUST be compared using StringComparer.Ordinal.
            var owinEnvironment = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                //Request http://owin.org/html/owin.html#3-2-1-request-data
                {"owin.RequestBody", requestStream},
                {"owin.RequestHeaders", request.Headers},
                {"owin.RequestMethod", request.Method},
                {"owin.RequestPath", uri.AbsolutePath},
                {"owin.RequestPathBase", "/"},
                {"owin.RequestProtocol", "HTTP/1.1"},
                {"owin.RequestQueryString", uri.Query},
                {"owin.RequestScheme", uri.Scheme},
                //Response http://owin.org/html/owin.html#3-2-2-response-data
                {"owin.ResponseHeaders", new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)},
                {"owin.ResponseBody", responseStream},
                //Other Data
                {"owin.Version", "1.0.0"},
                //{"owin.CallCancelled", cancellationToken}
            };

            await appFunc(owinEnvironment);

            //Response has been populated - reset the position to 0 so it can be read
            responseStream.Position = 0;

            int statusCode;
            string reasonPhrase = null;

            if (owinEnvironment.ContainsKey("owin.ResponseStatusCode"))
            {
                statusCode = Convert.ToInt32(owinEnvironment["owin.ResponseStatusCode"]);
            }
            else
            {
                statusCode = (int)System.Net.HttpStatusCode.OK;
            }

            if (owinEnvironment.ContainsKey("owin.ResponseReasonPhrase"))
            {
                reasonPhrase = owinEnvironment["owin.ResponseReasonPhrase"] as string;
            }

            if (string.IsNullOrEmpty(reasonPhrase))
            {
                reasonPhrase = "OK";
            }

            var responseHeaders = (Dictionary<string, string[]>)owinEnvironment["owin.ResponseHeaders"];

            return new ResourceResponse(statusCode, reasonPhrase, responseHeaders, responseStream);
        }
    }
}
