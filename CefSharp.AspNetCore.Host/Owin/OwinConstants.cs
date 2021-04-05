// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Originally from https://github.com/dotnet/aspnetcore/blob/c925f99cddac0df90ed0bc4a07ecda6b054a0b02/src/Http/Owin/src/OwinConstants.cs

namespace CefSharp.AspNetCore.Host.Owin
{
    public static class OwinConstants
    {
        // http://owin.org/spec/spec/owin-1.0.0.html
        public const string RequestScheme = "owin.RequestScheme";
        public const string RequestMethod = "owin.RequestMethod";
        public const string RequestPathBase = "owin.RequestPathBase";
        public const string RequestPath = "owin.RequestPath";
        public const string RequestQueryString = "owin.RequestQueryString";
        public const string RequestProtocol = "owin.RequestProtocol";
        public const string RequestHeaders = "owin.RequestHeaders";
        public const string RequestBody = "owin.RequestBody";

        // OWIN 1.0.1 http://owin.org/html/owin.html
        public const string RequestId = "owin.RequestId";
        public const string RequestUser = "owin.RequestUser";

        // http://owin.org/spec/spec/owin-1.0.0.html
        public const string ResponseStatusCode = "owin.ResponseStatusCode";
        public const string ResponseReasonPhrase = "owin.ResponseReasonPhrase";
        public const string ResponseProtocol = "owin.ResponseProtocol";
        public const string ResponseHeaders = "owin.ResponseHeaders";
        public const string ResponseBody = "owin.ResponseBody";

        public const string CallCancelled = "owin.CallCancelled";
    }
}
