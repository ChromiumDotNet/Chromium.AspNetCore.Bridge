using System;
using System.IO;
using System.Threading.Tasks;
using Chromely.Core;
using Chromely.Core.Configuration;
using Microsoft.AspNetCore.Hosting;
using CefSharp.AspNetCore.Host;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;


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
            AppFunc appFunc = null;

            //Only setup our AspNet Core host if within the Browser Process
            //Not needed for the sub processes (render, gpu, etc)
            //TODO: Move this somewhere internal to Chromely that
            //doesn't require the extra check and supports async
            if (!args.Any(x => x.StartsWith("--type")))
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

                appFunc = await tcs.Task;
            }

            var config = DefaultConfiguration.CreateForRuntimePlatform();
            config.WindowOptions.Title = "Title Window";
            config.StartUrl = "https://chromely.test";
            //config.StartUrl = "chrome://version";

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
