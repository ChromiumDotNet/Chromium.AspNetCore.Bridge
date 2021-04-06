using CefSharp.AspNetCore.Host;
using CefSharp.Wpf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CefSharp.AspNetCore.Mvc.Example.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IWebHost _host;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = new CefSettings();
            settings.CachePath = Path.Combine(Path.GetDirectoryName(typeof(App).Assembly.Location), "cache");
            Cef.Initialize(settings);

            _ = Task.Run(async () =>
              {
                  var builder = new WebHostBuilder();

                  builder.ConfigureServices(services =>
                  {
                      var server = new OwinServer();
                      server.UseOwin(appFunc =>
                      {
                          var requestContext = Cef.GetGlobalRequestContext();
                          requestContext.RegisterOwinSchemeHandlerFactory("https", "cefsharp.test", appFunc);
                      });

                      services.AddSingleton<IServer>(server);
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
