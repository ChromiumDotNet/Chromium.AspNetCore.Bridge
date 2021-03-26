using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CefSharp.AspNetCore.Mvc
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class OwinServer : IServer
    {
        private IFeatureCollection _features = new FeatureCollection();

        IFeatureCollection IServer.Features
        {
            get { return _features; }
        }

        void IDisposable.Dispose()
        {
            
        }

        Task IServer.StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            // Note that this example does not take into account of Nowin's "server.OnSendingHeaders" callback.
            // Ideally we should ensure this method is fired before disposing the context. 
            AppFunc appFunc = async env =>
            {
                var features = new FeatureCollection(new OwinFeatureCollection(env));

                var context = application.CreateContext(features);

                try
                {
                    await application.ProcessRequestAsync(context);
                }
                catch (Exception ex)
                {
                    application.DisposeContext(context, ex);
                    throw;
                }

                application.DisposeContext(context, null);
            };

            var requestContext = Cef.GetGlobalRequestContext();
            requestContext.RegisterOwinSchemeHandlerFactory("https", "cefsharp.test", appFunc); 

            return Task.CompletedTask;

        }

        Task IServer.StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}