using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace Chromely.AspNetCore.Mvc.Example.Owin
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class OwinSchemeHandlerFactory : CefSchemeHandlerFactory
    {
        private readonly AppFunc _appFunc;

        public OwinSchemeHandlerFactory(AppFunc appFunc)
        {
            _appFunc = appFunc;
        }

        protected override CefResourceHandler Create(CefBrowser browser, CefFrame frame, string schemeName, CefRequest request)
        {
            return new OwinResourceHandler(_appFunc);
        }
    }
}
