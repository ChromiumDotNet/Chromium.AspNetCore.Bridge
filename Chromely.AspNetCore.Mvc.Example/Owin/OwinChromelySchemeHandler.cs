using Chromely.Core;
using Chromely.Core.Network;

namespace Chromely.AspNetCore.Mvc.Example.Owin
{
    public class OwinChromelySchemeHandler : IChromelySchemeHandler
    {
        string IChromelySchemeHandler.Name { get; set; }
        UrlScheme IChromelySchemeHandler.Scheme { get; set; }
        object IChromelySchemeHandler.Handler { get; set; }
        object IChromelySchemeHandler.HandlerFactory { get; set; }
        bool IChromelySchemeHandler.IsCorsEnabled { get; set; }
        bool IChromelySchemeHandler.IsSecure { get; set; }
    }
}
