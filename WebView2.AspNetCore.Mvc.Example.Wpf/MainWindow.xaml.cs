using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Chromium.AspNetCore.Bridge;

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

            var request = new ResourceRequest(e.Request.Uri, e.Request.Method, e.Request.Headers, e.Request.Content);

            var response = await RequestInterceptor.ProcessRequest(_appFunc, request);

            var coreWebView2 = (CoreWebView2)sender;

            e.Response = coreWebView2.Environment.CreateWebResourceResponse(response.Stream, response.StatusCode, response.ReasonPhrase, response.GetHeaderString());

            deferral.Complete();
        }
    }
}
