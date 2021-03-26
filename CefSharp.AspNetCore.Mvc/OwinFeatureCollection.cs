using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Owin;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace CefSharp.AspNetCore.Mvc
{
    using SendFileFunc = Func<string, long, long?, CancellationToken, Task>;

    internal static class CommonKeys
    {
        public const string ClientCertificate = "ssl.ClientCertificate";
        public const string LoadClientCertAsync = "ssl.LoadClientCertAsync";
        public const string RemoteIpAddress = "server.RemoteIpAddress";
        public const string RemotePort = "server.RemotePort";
        public const string LocalIpAddress = "server.LocalIpAddress";
        public const string LocalPort = "server.LocalPort";
        public const string ConnectionId = "server.ConnectionId";
        public const string TraceOutput = "host.TraceOutput";
        public const string Addresses = "host.Addresses";
        public const string AppName = "host.AppName";
        public const string Capabilities = "server.Capabilities";
        public const string OnSendingHeaders = "server.OnSendingHeaders";
        public const string OnAppDisposing = "host.OnAppDisposing";
        public const string Scheme = "scheme";
        public const string Host = "host";
        public const string Port = "port";
        public const string Path = "path";
    }

    internal static class SendFiles
    {
        // 3.1. Startup

        public const string Version = "sendfile.Version";
        public const string Support = "sendfile.Support";
        public const string Concurrency = "sendfile.Concurrency";

        // 3.2. Per Request

        public const string SendAsync = "sendfile.SendAsync";
    }

    /// <summary>
    /// OWIN feature collection.
    /// </summary>
    public class OwinFeatureCollection :
        IFeatureCollection,
        IHttpRequestFeature,
        IHttpResponseFeature,
        IHttpResponseBodyFeature,
        IHttpConnectionFeature,
        IHttpRequestIdentifierFeature,
        IHttpRequestLifetimeFeature,
        IOwinEnvironmentFeature
    {
        

        /// <summary>
        /// Gets or sets OWIN environment values.
        /// </summary>
        public IDictionary<string, object> Environment { get; set; }
        private PipeWriter _responseBodyWrapper;
        private bool _headersSent;

        /// <summary>
        /// Initializes a new instance of <see cref="Microsoft.AspNetCore.Owin.OwinFeatureCollection"/>.
        /// </summary>
        /// <param name="environment">The environment values.</param>
        public OwinFeatureCollection(IDictionary<string, object> environment)
        {
            Environment = environment;

            var register = Prop<Action<Action<object>, object>>(CommonKeys.OnSendingHeaders);
            register?.Invoke(state =>
            {
                var collection = (OwinFeatureCollection)state;
                collection._headersSent = true;
            }, this);
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
            get { return MakeHeaderDictionary(Prop<IDictionary<string, string[]>>(OwinConstants.RequestHeaders)); }
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
            get { return MakeHeaderDictionary(Prop<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders)); }
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
            get { return _headersSent; }
        }

        void IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state)
        {
            
        }

        void IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state)
        {
            
        }

        IPAddress IHttpConnectionFeature.RemoteIpAddress
        {
            get { return IPAddress.Parse(Prop<string>(CommonKeys.RemoteIpAddress)); }
            set { Prop(CommonKeys.RemoteIpAddress, value.ToString()); }
        }

        IPAddress IHttpConnectionFeature.LocalIpAddress
        {
            get { return IPAddress.Parse(Prop<string>(CommonKeys.LocalIpAddress)); }
            set { Prop(CommonKeys.LocalIpAddress, value.ToString()); }
        }

        int IHttpConnectionFeature.RemotePort
        {
            get { return int.Parse(Prop<string>(CommonKeys.RemotePort), CultureInfo.InvariantCulture); }
            set { Prop(CommonKeys.RemotePort, value.ToString(CultureInfo.InvariantCulture)); }
        }

        int IHttpConnectionFeature.LocalPort
        {
            get { return int.Parse(Prop<string>(CommonKeys.LocalPort), CultureInfo.InvariantCulture); }
            set { Prop(CommonKeys.LocalPort, value.ToString(CultureInfo.InvariantCulture)); }
        }

        string IHttpConnectionFeature.ConnectionId
        {
            get { return Prop<string>(CommonKeys.ConnectionId); }
            set { Prop(CommonKeys.ConnectionId, value); }
        }

        Task IHttpResponseBodyFeature.SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            object obj;
            if (Environment.TryGetValue(SendFiles.SendAsync, out obj))
            {
                var func = (SendFileFunc)obj;
                return func(path, offset, length, cancellation);
            }
            throw new NotSupportedException(SendFiles.SendAsync);
        }

        CancellationToken IHttpRequestLifetimeFeature.RequestAborted
        {
            get { return Prop<CancellationToken>(OwinConstants.CallCancelled); }
            set { Prop(OwinConstants.CallCancelled, value); }
        }

        void IHttpRequestLifetimeFeature.Abort()
        {
            throw new NotImplementedException();
        }

        // IFeatureCollection

        /// <inheritdoc/>
        public int Revision
        {
            get { return 0; } // Not modifiable
        }

        /// <inheritdoc/>
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public object this[Type key]
        {
            get { return Get(key); }
            set { throw new NotSupportedException(); }
        }

        private bool SupportsInterface(Type key)
        {
            // Does this type implement the requested interface?
            if (key.IsAssignableFrom(GetType()))
            {
                // Check for conditional features
                if (key == typeof(ITlsConnectionFeature))
                {
                    return false;
                }
                else if (key == typeof(IHttpWebSocketFeature))
                {
                    return false;
                }

                // The rest of the features are always supported.
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public object Get(Type key)
        {
            if (SupportsInterface(key))
            {
                return this;
            }
            return null;
        }

        /// <inheritdoc/>
        public void Set(Type key, object value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public TFeature Get<TFeature>()
        {
            return (TFeature)this[typeof(TFeature)];
        }

        /// <inheritdoc/>
        public void Set<TFeature>(TFeature instance)
        {
            this[typeof(TFeature)] = instance;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            yield return new KeyValuePair<Type, object>(typeof(IHttpRequestFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpResponseFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpResponseBodyFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpConnectionFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpRequestIdentifierFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpRequestLifetimeFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IOwinEnvironmentFeature), this);
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

        private static IHeaderDictionary MakeHeaderDictionary(IDictionary<string, string[]> dictionary)
        {
            var wrapper = dictionary as DictionaryStringArrayWrapper;
            if (wrapper != null)
            {
                return wrapper.Inner;
            }
            return new DictionaryStringValuesWrapper(dictionary);
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

    internal class DictionaryStringValuesWrapper : IHeaderDictionary
    {
        public DictionaryStringValuesWrapper(IDictionary<string, string[]> inner)
        {
            Inner = inner;
        }

        public readonly IDictionary<string, string[]> Inner;

        private KeyValuePair<string, StringValues> Convert(KeyValuePair<string, string[]> item) => new KeyValuePair<string, StringValues>(item.Key, item.Value);

        private KeyValuePair<string, string[]> Convert(KeyValuePair<string, StringValues> item) => new KeyValuePair<string, string[]>(item.Key, item.Value);

        private StringValues Convert(string[] item) => item;

        private string[] Convert(StringValues item) => item;

        StringValues IHeaderDictionary.this[string key]
        {
            get
            {
                string[] values;
                return Inner.TryGetValue(key, out values) ? values : null;
            }
            set { Inner[key] = value; }
        }

        StringValues IDictionary<string, StringValues>.this[string key]
        {
            get { return Inner[key]; }
            set { Inner[key] = value; }
        }

        public long? ContentLength
        {
            get
            {
                long value;

                string[] rawValue;
                if (!Inner.TryGetValue(HeaderNames.ContentLength, out rawValue))
                {
                    return null;
                }

                if (rawValue.Length == 1 &&
                    !string.IsNullOrEmpty(rawValue[0]) &&
                    HeaderUtilities.TryParseNonNegativeInt64(new StringSegment(rawValue[0]).Trim(), out value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    Inner[HeaderNames.ContentLength] = (StringValues)HeaderUtilities.FormatNonNegativeInt64(value.GetValueOrDefault());
                }
                else
                {
                    Inner.Remove(HeaderNames.ContentLength);
                }
            }
        }

        int ICollection<KeyValuePair<string, StringValues>>.Count => Inner.Count;

        bool ICollection<KeyValuePair<string, StringValues>>.IsReadOnly => Inner.IsReadOnly;

        ICollection<string> IDictionary<string, StringValues>.Keys => Inner.Keys;

        ICollection<StringValues> IDictionary<string, StringValues>.Values => Inner.Values.Select(Convert).ToList();

        void ICollection<KeyValuePair<string, StringValues>>.Add(KeyValuePair<string, StringValues> item) => Inner.Add(Convert(item));

        void IDictionary<string, StringValues>.Add(string key, StringValues value) => Inner.Add(key, value);

        void ICollection<KeyValuePair<string, StringValues>>.Clear() => Inner.Clear();

        bool ICollection<KeyValuePair<string, StringValues>>.Contains(KeyValuePair<string, StringValues> item) => Inner.Contains(Convert(item));

        bool IDictionary<string, StringValues>.ContainsKey(string key) => Inner.ContainsKey(key);

        void ICollection<KeyValuePair<string, StringValues>>.CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            foreach (var kv in Inner)
            {
                array[arrayIndex++] = Convert(kv);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => Inner.Select(Convert).GetEnumerator();

        IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator() => Inner.Select(Convert).GetEnumerator();

        bool ICollection<KeyValuePair<string, StringValues>>.Remove(KeyValuePair<string, StringValues> item) => Inner.Remove(Convert(item));

        bool IDictionary<string, StringValues>.Remove(string key) => Inner.Remove(key);

        bool IDictionary<string, StringValues>.TryGetValue(string key, out StringValues value)
        {
            string[] temp;
            if (Inner.TryGetValue(key, out temp))
            {
                value = temp;
                return true;
            }
            value = default(StringValues);
            return false;
        }
    }

    internal class DictionaryStringArrayWrapper : IDictionary<string, string[]>
    {
        public DictionaryStringArrayWrapper(IHeaderDictionary inner)
        {
            Inner = inner;
        }

        public readonly IHeaderDictionary Inner;

        private KeyValuePair<string, StringValues> Convert(KeyValuePair<string, string[]> item) => new KeyValuePair<string, StringValues>(item.Key, item.Value);

        private KeyValuePair<string, string[]> Convert(KeyValuePair<string, StringValues> item) => new KeyValuePair<string, string[]>(item.Key, item.Value);

        private StringValues Convert(string[] item) => item;

        private string[] Convert(StringValues item) => item;

        string[] IDictionary<string, string[]>.this[string key]
        {
            get { return ((IDictionary<string, StringValues>)Inner)[key]; }
            set { Inner[key] = value; }
        }

        int ICollection<KeyValuePair<string, string[]>>.Count => Inner.Count;

        bool ICollection<KeyValuePair<string, string[]>>.IsReadOnly => Inner.IsReadOnly;

        ICollection<string> IDictionary<string, string[]>.Keys => Inner.Keys;

        ICollection<string[]> IDictionary<string, string[]>.Values => Inner.Values.Select(Convert).ToList();

        void ICollection<KeyValuePair<string, string[]>>.Add(KeyValuePair<string, string[]> item) => Inner.Add(Convert(item));

        void IDictionary<string, string[]>.Add(string key, string[] value) => Inner.Add(key, value);

        void ICollection<KeyValuePair<string, string[]>>.Clear() => Inner.Clear();

        bool ICollection<KeyValuePair<string, string[]>>.Contains(KeyValuePair<string, string[]> item) => Inner.Contains(Convert(item));

        bool IDictionary<string, string[]>.ContainsKey(string key) => Inner.ContainsKey(key);

        void ICollection<KeyValuePair<string, string[]>>.CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex)
        {
            foreach (var kv in Inner)
            {
                array[arrayIndex++] = Convert(kv);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => Inner.Select(Convert).GetEnumerator();

        IEnumerator<KeyValuePair<string, string[]>> IEnumerable<KeyValuePair<string, string[]>>.GetEnumerator() => Inner.Select(Convert).GetEnumerator();

        bool ICollection<KeyValuePair<string, string[]>>.Remove(KeyValuePair<string, string[]> item) => Inner.Remove(Convert(item));

        bool IDictionary<string, string[]>.Remove(string key) => Inner.Remove(key);

        bool IDictionary<string, string[]>.TryGetValue(string key, out string[] value)
        {
            StringValues temp;
            if (Inner.TryGetValue(key, out temp))
            {
                value = temp;
                return true;
            }
            value = default(StringValues);
            return false;
        }
    }
}

