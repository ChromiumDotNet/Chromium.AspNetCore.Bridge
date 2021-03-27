// Copyright (c) Alex Maitland. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

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
        private Action<AppFunc> useOwin;

        IFeatureCollection IServer.Features
        {
            get { return _features; }
        }

        void IDisposable.Dispose()
        {
            
        }

        public void UseOwin(Action<AppFunc> action)
        {
            useOwin = action;
        }

        Task IServer.StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
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

            useOwin?.Invoke(appFunc);

            return Task.CompletedTask;
        }

        Task IServer.StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}