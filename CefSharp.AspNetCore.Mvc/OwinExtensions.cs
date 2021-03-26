using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CefSharp.AspNetCore.Mvc
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public static class OwinExtensions
    {
        public static void RegisterOwinSchemeHandlerFactory(this IRequestContext requestContext, string schemeName, string domainName, AppFunc appFunc)
        {
            requestContext.RegisterSchemeHandlerFactory(schemeName, domainName, new OwinSchemeHandlerFactory(appFunc) );
        }

        public static void RegisterOwinSchemeHandlerFactory(this CefSettingsBase settings, string schemeName, string domainName, AppFunc appFunc)
        {
            settings.RegisterScheme(new CefCustomScheme { SchemeName = schemeName, DomainName = domainName, SchemeHandlerFactory = new OwinSchemeHandlerFactory(appFunc) });
        }
    }
}
