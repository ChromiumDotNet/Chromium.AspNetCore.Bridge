// Copyright (c) Alex Maitland. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Chromium.AspNetCore.Bridge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace Chromely.AspNetCore.Mvc.Example.Owin
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// Loosly based on https://github.com/eVisionSoftware/Harley/blob/master/src/Harley.UI/Owin/OwinSchemeHandlerFactory.cs
    /// New instance is instanciated for every request
    /// </summary>
    public class OwinResourceHandler : ResourceHandler
    {
        private static readonly Dictionary<int, string> StatusCodeToStatusTextMapping = new Dictionary<int, string>
        {
            {200, "OK"},
            {301, "Moved Permanently"},
            {304, "Not Modified"},
            {404, "Not Found"}
        };

        private readonly AppFunc _appFunc;

        public OwinResourceHandler(AppFunc appFunc)
        {
            _appFunc = appFunc;
        }

        /// <summary>
        /// Read the request, then process it through the OWIN pipeline
        /// then populate the response properties.
        /// </summary>
        /// <param name="request">request</param>
        /// <param name="callback">callback</param>
        /// <returns>always returns true as we'll handle all requests this handler is registered for.</returns>
        public override CefReturnValue ProcessRequestAsync(CefRequest request, CefCallback callback)
        {
            var requestBody = Stream.Null;

            if (request.Method == "POST")
            {
                using (var postData = request.PostData)
                {
                    if (postData != null)
                    {
                        var postDataElements = postData.GetElements();

                        var firstPostDataElement = postDataElements.First();

                        var bytes = firstPostDataElement.GetBytes();

                        requestBody = new MemoryStream(bytes, 0, bytes.Length);

                        //TODO: Investigate how to process multi part POST data
                        //var charSet = request.GetCharSet();
                        //foreach (var element in elements)
                        //{
                        //    if (element.Type == PostDataElementType.Bytes)
                        //    {
                        //        var body = element.GetBody(charSet);
                        //    }
                        //}
                    }
                }
            }

            var uri = new Uri(request.Url);
            var requestHeaders = request.GetHeaderMap();
            //Add Host header as per http://owin.org/html/owin.html#5-2-hostname
            requestHeaders.Add("Host", uri.Host + (uri.Port > 0 ? (":" + uri.Port) : ""));

            var owinRequest = new ResourceRequest(request.Url, request.Method, requestHeaders, requestBody);

            //PART 2 - Spawn a new task to execute the OWIN pipeline
            //We execute this in an async fashion and return true so other processing
            //can occur
            Task.Run(async () =>
            {
                try
                {
                    //Call into the OWIN pipeline
                    var owinResponse = await RequestInterceptor.ProcessRequest(_appFunc, owinRequest);

                    //Populate the response properties
                    Stream = owinResponse.Stream;
                    ResponseLength = owinResponse.Stream.Length;
                    StatusCode = owinResponse.StatusCode;

                    var responseHeaders = owinResponse.Headers;

                    if (responseHeaders.ContainsKey("Content-Type"))
                    {
                        var contentType = responseHeaders["Content-Type"].First();
                        MimeType = contentType.Split(';').First();
                    }
                    else
                    {
                        MimeType = DefaultMimeType;
                    }                    

                    //Add the response headers from OWIN to the Headers NameValueCollection
                    foreach (var responseHeader in responseHeaders)
                    {
                        //It's possible for headers to have multiple values
                        foreach (var val in responseHeader.Value)
                        {
                            Headers.Add(responseHeader.Key, val);
                        }
                    }
                }
                catch(Exception ex)
                {
                    int statusCode = (int)HttpStatusCode.InternalServerError;

                    var responseData = GetByteArray("Error: " + ex.ToString(), System.Text.Encoding.UTF8, true);

                    //Populate the response properties
                    Stream = new MemoryStream(responseData);
                    ResponseLength = responseData.Length;
                    StatusCode = statusCode;
                    MimeType = "text/html";
                }

                //Once we've finished populating the properties we execute the callback
                //Callback wraps an unmanaged resource, so let's explicitly Dispose when we're done    
                using (callback)
                {
                    callback.Continue();
                }
            });

            return CefReturnValue.ContinueAsync;
        }
    }
}
