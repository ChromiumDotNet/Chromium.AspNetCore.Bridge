using System;
using System.IO;
using System.Threading.Tasks;
using Chromely.Core;
using Chromely.Core.Configuration;
using Microsoft.AspNetCore.Hosting;
using CefSharp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Chromely.Core.Network;
using Xilium.CefGlue;
using Chromely.AspNetCore.Mvc.Example.Owin;

namespace Chromely.AspNetCore.Mvc.Example
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class Program
    {
        private static IWebHost _host;

        /// <summary> Main entry-point for this application. </summary>
        /// <param name="args"> The arguments to pass in. </param>
        [STAThread]
        public static async Task Main(string[] args)
        {
            var tcs = new TaskCompletionSource<AppFunc>();

            var builder = new WebHostBuilder();

            builder.ConfigureServices(services =>
            {
                var server = new OwinServer();
                server.UseOwin(appFunc =>
                {
                    tcs.SetResult(appFunc);
                });

                services.AddSingleton<IServer>(server);
            });

            _host = builder
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            _ = _host.RunAsync();

            var appFunc = await tcs.Task;

            var config = DefaultConfiguration.CreateForRuntimePlatform();
            config.WindowOptions.Title = "Title Window";
            config.StartUrl = "https://chromely.test";

            var app = new OwinChromelyBasicApp(appFunc);

            AppBuilder
                .Create()
                .UseConfig<DefaultConfiguration>(config)
                .UseApp<OwinChromelyBasicApp>(app)
                .Build()
                .Run(args);

            await _host.StopAsync();
        }

    }
}
