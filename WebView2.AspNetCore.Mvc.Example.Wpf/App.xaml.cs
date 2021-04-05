using CefSharp.AspNetCore.Host;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace WebView2.AspNetCore.Mvc.Example.Wpf
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IWebHost _host;
        private AppFunc _appFunc;

        public AppFunc AppFunc
        {
            get { return _appFunc; }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

            _ = Task.Run(async () =>
              {
                  var builder = new WebHostBuilder();

                  builder.ConfigureServices(services =>
                  {
                      var server = new OwinServer();
                      server.UseOwin(appFunc =>
                      {
                          _appFunc = appFunc;
                      });

                      services.AddSingleton<IServer>(server);
                  });

                  builder.ConfigureLogging(logging =>
                  {
                      logging.AddConsole();
                  });

                  _host = builder
                      .UseStartup<Startup>()
                      .UseContentRoot(Directory.GetCurrentDirectory())
                      .Build();

                  await _host.RunAsync();
              });            
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host.StopAsync();

            base.OnExit(e);
        }
    }
}
