using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WebView2.AspNetCore.Mvc.Example.Wpf
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppFunc _appFunc;

        public MainWindow()
        {
            InitializeComponent();

            Browser.CoreWebView2InitializationCompleted += Browser_CoreWebView2InitializationCompleted;
        }

        private void Browser_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                _appFunc = ((App)Application.Current).AppFunc;
                Browser.CoreWebView2.WebResourceRequested += BrowserWebResourceRequestedAsync;
                Browser.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            }
        }

        private async void BrowserWebResourceRequestedAsync(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();

            var requestStream = Stream.Null;
            var responseStream = new MemoryStream();

            //Take a copy of our stream, otherwise getting a Read Error from the COM Stream wrapper
            //We need a seekable stream and the COM Stream wrapper isn't
            if (e.Request.Content != null)
            {
                requestStream = new MemoryStream();
                e.Request.Content.CopyTo(requestStream);
                requestStream.Position = 0;
            }

            var uri = new Uri(e.Request.Uri);
            var requestHeaders = ToDictionary(e.Request.Headers);

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
                {"owin.RequestHeaders", requestHeaders},
                {"owin.RequestMethod", e.Request.Method},
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

            await _appFunc(owinEnvironment);

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

            if(string.IsNullOrEmpty(reasonPhrase))
            {
                reasonPhrase = "OK";
            }            

            //Grab a reference to the ResponseHeaders
            var responseHeaders = (Dictionary<string, string[]>)owinEnvironment["owin.ResponseHeaders"];
            var headers = new StringBuilder();
            
            //Add the response headers from OWEN to the Response Headers
            foreach (var responseHeader in responseHeaders)
            {
                //It's possible for headers to have multiple values
                foreach (var val in responseHeader.Value)
                {
                    headers.AppendLine(responseHeader.Key + "=" + val);
                }
            }

            e.Response = Browser.CoreWebView2.Environment.CreateWebResourceResponse(responseStream, statusCode, reasonPhrase, headers.ToString());

            deferral.Complete();
        }

        private static IDictionary<string, string[]> ToDictionary(CoreWebView2HttpRequestHeaders requestHeaders)
        {
            var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in requestHeaders)
            {
                //TODO: Deal with requestHeaders.GetHeaders(header.Key)

                dict.Add(header.Key, new string[] { header.Value });

                //if (!dict.ContainsKey(header.Key))
                //{
                //    dict.Add(header.Key, new string[0]);
                //}
                //var strings = ;
                //if (strings == null)
                //{
                //    continue;
                //}
                //foreach (string value in strings)
                //{
                //    var values = dict[key].ToList();
                //    values.Add(value);
                //    dict[key] = values.ToArray();
                //}
            }
            return dict;
        }
    }
}
