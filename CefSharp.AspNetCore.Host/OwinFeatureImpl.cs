// Copyright (c) Alex Maitland. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using CefSharp.AspNetCore.Host.Owin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Owin;

namespace CefSharp.AspNetCore.Host
{
    /// <summary>
    /// CefSharp OWIN feature collection.
    /// A simplified set of features that can be adapted directly in CefSharp (potentially other web browsers)
    /// </summary>
    public class OwinFeatureImpl :
        IHttpRequestFeature,
        IHttpResponseFeature,
        IHttpResponseBodyFeature,
        IHttpRequestIdentifierFeature,
        IOwinEnvironmentFeature
    {
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

        private T GetEnvironmentPropertyOrDefault<T>(string key)
        {
            object value;
            if (Environment.TryGetValue(key, out value) && value is T)
            {
                return (T)value;
            }
            return default(T);
        }

        private void SetEnvironmentProperty(string key, object value)
        {
            Environment[key] = value;
        }

        string IHttpRequestFeature.Protocol
        {
            get { return GetEnvironmentPropertyOrDefault<string>(OwinConstants.RequestProtocol); }
            set { SetEnvironmentProperty(OwinConstants.RequestProtocol, value); }
        }

        string IHttpRequestFeature.Scheme
        {
            get { return GetEnvironmentPropertyOrDefault<string>(OwinConstants.RequestScheme); }
            set { SetEnvironmentProperty(OwinConstants.RequestScheme, value); }
        }

        string IHttpRequestFeature.Method
        {
            get { return GetEnvironmentPropertyOrDefault<string>(OwinConstants.RequestMethod); }
            set { SetEnvironmentProperty(OwinConstants.RequestMethod, value); }
        }

        string IHttpRequestFeature.PathBase
        {
            get { return GetEnvironmentPropertyOrDefault<string>(OwinConstants.RequestPathBase); }
            set { SetEnvironmentProperty(OwinConstants.RequestPathBase, value); }
        }

        string IHttpRequestFeature.Path
        {
            get { return GetEnvironmentPropertyOrDefault<string>(OwinConstants.RequestPath); }
            set { SetEnvironmentProperty(OwinConstants.RequestPath, value); }
        }

        string IHttpRequestFeature.QueryString
        {
            get { return AddQuestionMark(GetEnvironmentPropertyOrDefault<string>(OwinConstants.RequestQueryString)); }
            set { SetEnvironmentProperty(OwinConstants.RequestQueryString, RemoveQuestionMark(value)); }
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
                    var dict = GetEnvironmentPropertyOrDefault<IDictionary<string, string[]>>(OwinConstants.RequestHeaders);
                    _requestHeaders = dict is IHeaderDictionary ? (IHeaderDictionary)dict : new DictionaryStringValuesWrapper(dict);
                }
                return _requestHeaders;
            }
            set { SetEnvironmentProperty(OwinConstants.RequestHeaders, MakeDictionaryStringArray(value)); }
        }

        string IHttpRequestIdentifierFeature.TraceIdentifier
        {
            get { return GetEnvironmentPropertyOrDefault<string>(OwinConstants.RequestId); }
            set { SetEnvironmentProperty(OwinConstants.RequestId, value); }
        }

        Stream IHttpRequestFeature.Body
        {
            get { return GetEnvironmentPropertyOrDefault<Stream>(OwinConstants.RequestBody); }
            set { SetEnvironmentProperty(OwinConstants.RequestBody, value); }
        }

        int IHttpResponseFeature.StatusCode
        {
            get { return GetEnvironmentPropertyOrDefault<int>(OwinConstants.ResponseStatusCode); }
            set { SetEnvironmentProperty(OwinConstants.ResponseStatusCode, value); }
        }

        string IHttpResponseFeature.ReasonPhrase
        {
            get { return GetEnvironmentPropertyOrDefault<string>(OwinConstants.ResponseReasonPhrase); }
            set { SetEnvironmentProperty(OwinConstants.ResponseReasonPhrase, value); }
        }

        IHeaderDictionary IHttpResponseFeature.Headers
        {
            get
            {
                if (_responseHeaders == null)
                {
                    var dict = GetEnvironmentPropertyOrDefault<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders);
                    _responseHeaders = dict is IHeaderDictionary ? (IHeaderDictionary)dict : new DictionaryStringValuesWrapper(dict);
                }
                return _responseHeaders;
            }
            set { SetEnvironmentProperty(OwinConstants.ResponseHeaders, MakeDictionaryStringArray(value)); }
        }

        Stream IHttpResponseFeature.Body
        {
            get { return GetEnvironmentPropertyOrDefault<Stream>(OwinConstants.ResponseBody); }
            set { SetEnvironmentProperty(OwinConstants.ResponseBody, value); }
        }

        Stream IHttpResponseBodyFeature.Stream
        {
            get { return GetEnvironmentPropertyOrDefault<Stream>(OwinConstants.ResponseBody); }
        }

        PipeWriter IHttpResponseBodyFeature.Writer
        {
            get
            {
                if (_responseBodyWrapper == null)
                {
                    _responseBodyWrapper = PipeWriter.Create(GetEnvironmentPropertyOrDefault<Stream>(OwinConstants.ResponseBody), new StreamPipeWriterOptions(leaveOpen: true));
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
            await GetEnvironmentPropertyOrDefault<Stream>(OwinConstants.ResponseBody).FlushAsync(cancellationToken);
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

