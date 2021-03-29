using Chromely.AspNetCore.Mvc.Example.Owin;
using Chromely.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace Chromely.AspNetCore.Mvc.Example
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class OwinChromelyBasicApp : ChromelyBasicApp
    {
        public OwinChromelyBasicApp(AppFunc appFunc)
        {
            AppFunc = appFunc;
        }

        public AppFunc AppFunc { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            IChromelySchemeHandler chromelyCustomScheme = new OwinChromelySchemeHandler();
            chromelyCustomScheme.Name = "Test";
            chromelyCustomScheme.IsSecure = true;
            chromelyCustomScheme.Scheme = new Core.Network.UrlScheme("https", "chromely.test", Core.Network.UrlSchemeType.LocalRquest);
            chromelyCustomScheme.IsCorsEnabled = true;
            chromelyCustomScheme.HandlerFactory = new OwinSchemeHandlerFactory(AppFunc);

            services.AddSingleton(chromelyCustomScheme);
        }
    }
}
