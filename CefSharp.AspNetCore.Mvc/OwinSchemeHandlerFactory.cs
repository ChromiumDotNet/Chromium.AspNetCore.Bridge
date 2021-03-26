using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CefSharp.AspNetCore.Mvc
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class OwinSchemeHandlerFactory : ISchemeHandlerFactory
    {
        private readonly AppFunc _appFunc;

        public OwinSchemeHandlerFactory(AppFunc appFunc)
        {
            _appFunc = appFunc;
        }

        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new OwinResourceHandler(_appFunc);
        }
    }
}
