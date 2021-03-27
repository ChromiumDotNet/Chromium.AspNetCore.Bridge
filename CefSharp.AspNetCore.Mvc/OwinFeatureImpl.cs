// Copyright (c) Alex Maitland. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CefSharp.AspNetCore.Mvc.Owin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Owin;

namespace CefSharp.AspNetCore.Mvc
{
    /// <summary>
    /// CefSharp OWIN feature collection.
    /// A simplified set of features that can be adapted directly in CefSharp (potentially other web browsers)
    /// </summary>
    public class OwinFeatureImpl :
        IHttpRequestFeature,
        IHttpResponseFeature,
        IHttpResponseBodyFeature,
        IHttpConnectionFeature,
        IHttpRequestIdentifierFeature,
        IOwinEnvironmentFeature
    {
        private const string RemoteIpAddress = "server.RemoteIpAddress";
        private const string RemotePort = "server.RemotePort";
        private const string LocalIpAddress = "server.LocalIpAddress";
        private const string LocalPort = "server.LocalPort";
        private const string ConnectionId = "server.ConnectionId";

        /// <summary>
        /// Gets or sets OWIN environment values.
        /// </summary>
        public IDictionary<string, object> Environment { get; set; }
        private PipeWriter _responseBodyWrapper;
        private IHeaderDictionary _requestHeaders;
        private IHeaderDictionary _responseHeaders;

        /// <summary>
        /// Initializes a new instance of <see cref="Microsoft.AspNetCore.Owin.OwinFeatureCollection"/>.
        /// </summary>
        /// <param name="environment">The environment values.</param>
        public OwinFeatureImpl(IDictionary<string, object> environment)
        {
            Environment = environment;
        }

        T Prop<T>(string key)
        {
            object value;
            if (Environment.TryGetValue(key, out value) && value is T)
            {
                return (T)value;
            }
            return default(T);
        }

        void Prop(string key, object value)
        {
            Environment[key] = value;
        }

        string IHttpRequestFeature.Protocol
        {
            get { return Prop<string>(OwinConstants.RequestProtocol); }
            set { Prop(OwinConstants.RequestProtocol, value); }
        }

        string IHttpRequestFeature.Scheme
        {
            get { return Prop<string>(OwinConstants.RequestScheme); }
            set { Prop(OwinConstants.RequestScheme, value); }
        }

        string IHttpRequestFeature.Method
        {
            get { return Prop<string>(OwinConstants.RequestMethod); }
            set { Prop(OwinConstants.RequestMethod, value); }
        }

        string IHttpRequestFeature.PathBase
        {
            get { return Prop<string>(OwinConstants.RequestPathBase); }
            set { Prop(OwinConstants.RequestPathBase, value); }
        }

        string IHttpRequestFeature.Path
        {
            get { return Prop<string>(OwinConstants.RequestPath); }
            set { Prop(OwinConstants.RequestPath, value); }
        }

        string IHttpRequestFeature.QueryString
        {
            get { return AddQuestionMark(Prop<string>(OwinConstants.RequestQueryString)); }
            set { Prop(OwinConstants.RequestQueryString, RemoveQuestionMark(value)); }
        }

        string IHttpRequestFeature.RawTarget
        {
            get { return string.Empty; }
            set { throw new NotSupportedException(); }
        }

        IHeaderDictionary IHttpRequestFeature.Headers
        {
            get
            {
                if(_requestHeaders == null)
                {
                    var dict = Prop<IDictionary<string, string[]>>(OwinConstants.RequestHeaders);
                    _requestHeaders = dict is IHeaderDictionary ? (IHeaderDictionary)dict : new DictionaryStringValuesWrapper(dict);
                }
                return _requestHeaders;
            }
            set { Prop(OwinConstants.RequestHeaders, MakeDictionaryStringArray(value)); }
        }

        string IHttpRequestIdentifierFeature.TraceIdentifier
        {
            get { return Prop<string>(OwinConstants.RequestId); }
            set { Prop(OwinConstants.RequestId, value); }
        }

        Stream IHttpRequestFeature.Body
        {
            get { return Prop<Stream>(OwinConstants.RequestBody); }
            set { Prop(OwinConstants.RequestBody, value); }
        }

        int IHttpResponseFeature.StatusCode
        {
            get { return Prop<int>(OwinConstants.ResponseStatusCode); }
            set { Prop(OwinConstants.ResponseStatusCode, value); }
        }

        string IHttpResponseFeature.ReasonPhrase
        {
            get { return Prop<string>(OwinConstants.ResponseReasonPhrase); }
            set { Prop(OwinConstants.ResponseReasonPhrase, value); }
        }

        IHeaderDictionary IHttpResponseFeature.Headers
        {
            get
            {
                if (_responseHeaders == null)
                {
                    var dict = Prop<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders);
                    _responseHeaders = dict is IHeaderDictionary ? (IHeaderDictionary)dict : new DictionaryStringValuesWrapper(dict);
                }
                return _responseHeaders;
            }
            set { Prop(OwinConstants.ResponseHeaders, MakeDictionaryStringArray(value)); }
        }

        Stream IHttpResponseFeature.Body
        {
            get { return Prop<Stream>(OwinConstants.ResponseBody); }
            set { Prop(OwinConstants.ResponseBody, value); }
        }

        Stream IHttpResponseBodyFeature.Stream
        {
            get { return Prop<Stream>(OwinConstants.ResponseBody); }
        }

        PipeWriter IHttpResponseBodyFeature.Writer
        {
            get
            {
                if (_responseBodyWrapper == null)
                {
                    _responseBodyWrapper = PipeWriter.Create(Prop<Stream>(OwinConstants.ResponseBody), new StreamPipeWriterOptions(leaveOpen: true));
                }

                return _responseBodyWrapper;
            }
        }

        bool IHttpResponseFeature.HasStarted
        {
            get { return false; }
        }

        void IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state)
        {
            
        }

        void IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state)
        {
            
        }

        IPAddress IHttpConnectionFeature.RemoteIpAddress
        {
            get { return IPAddress.Parse(Prop<string>(RemoteIpAddress)); }
            set { Prop(RemoteIpAddress, value.ToString()); }
        }

        IPAddress IHttpConnectionFeature.LocalIpAddress
        {
            get { return IPAddress.Parse(Prop<string>(LocalIpAddress)); }
            set { Prop(LocalIpAddress, value.ToString()); }
        }

        int IHttpConnectionFeature.RemotePort
        {
            get { return int.Parse(Prop<string>(RemotePort), CultureInfo.InvariantCulture); }
            set { Prop(RemotePort, value.ToString(CultureInfo.InvariantCulture)); }
        }

        int IHttpConnectionFeature.LocalPort
        {
            get { return int.Parse(Prop<string>(LocalPort), CultureInfo.InvariantCulture); }
            set { Prop(LocalPort, value.ToString(CultureInfo.InvariantCulture)); }
        }

        string IHttpConnectionFeature.ConnectionId
        {
            get { return Prop<string>(ConnectionId); }
            set { Prop(ConnectionId, value); }
        }

        Task IHttpResponseBodyFeature.SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            var writer = ((IHttpResponseBodyFeature)this).Writer;

            return SendFileFallback.SendFileAsync(writer.AsStream(), path, offset, length, cancellation);
        }

        void IHttpResponseBodyFeature.DisableBuffering()
        {
        }

        async Task IHttpResponseBodyFeature.StartAsync(CancellationToken cancellationToken)
        {
            if (_responseBodyWrapper != null)
            {
                await _responseBodyWrapper.FlushAsync(cancellationToken);
            }

            // The pipe may or may not have flushed the stream. Make sure the stream gets flushed to trigger response start.
            await Prop<Stream>(OwinConstants.ResponseBody).FlushAsync(cancellationToken);
        }

        Task IHttpResponseBodyFeature.CompleteAsync()
        {
            if (_responseBodyWrapper != null)
            {
                return _responseBodyWrapper.FlushAsync().AsTask();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        private static string RemoveQuestionMark(string queryString)
        {
            if (!string.IsNullOrEmpty(queryString))
            {
                if (queryString[0] == '?')
                {
                    return queryString.Substring(1);
                }
            }
            return queryString;
        }

        private static string AddQuestionMark(string queryString)
        {
            if (!string.IsNullOrEmpty(queryString))
            {
                return '?' + queryString;
            }
            return queryString;
        }

        private static IDictionary<string, string[]> MakeDictionaryStringArray(IHeaderDictionary dictionary)
        {
            var wrapper = dictionary as DictionaryStringValuesWrapper;
            if (wrapper != null)
            {
                return wrapper.Inner;
            }
            return new DictionaryStringArrayWrapper(dictionary);
        }
    }
}

