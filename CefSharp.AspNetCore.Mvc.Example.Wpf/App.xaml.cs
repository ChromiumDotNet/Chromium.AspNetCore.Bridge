using CefSharp.Wpf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
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

            var builder = new WebHostBuilder();

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IServer, OwinServer>();
            });

            _host = builder
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();
            _host.RunAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host.StopAsync();

            base.OnExit(e);
        }
    }
}
