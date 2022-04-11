using Avalonia;
using Dynaframe3.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dynaframe3.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            ConfigureServices(args);
            using (var db = new MediaDataContext())
            {
                db.Database.Migrate();
            }

            var host = HttpHost.CreateHostBuilder(args);
            await host.StartAsync();

            await Locator.Current.GetService<DeviceCache>().InitializeAsync();
            var y = SynchronizationContext.Current;

            BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
            
            // Avalonia cretaes it's own SerializationContext that it doesn't remove afterward. Since everything
            // is already done from Avalonia's standpoint. This causes a deadlock with the following async calls
            // so we need to clean it up ourselves.
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                await host.StopAsync();
            }
            finally
            {
                await host.DisposeAsync();
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .With(new AvaloniaNativePlatformOptions { UseGpu = true })
            .LogToTrace();

        public static void ConfigureServices(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddCommandLine(args);
            configurationBuilder.AddEnvironmentVariables();

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(sp => configurationBuilder.Build());

            services.AddHttpClient("", configureClient: h =>
            {
                h.BaseAddress = new Uri(Environment.GetEnvironmentVariable("DYNAFRAME_SERVER"));
            })
                .AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(250)))
                ;

            services.AddSingleton<DeviceCache>();

            services.AddSingleton(sp =>
            {
                var settings = new HttpSettings();
                var config = sp.GetRequiredService<IConfiguration>();
                config.Bind(settings);
                return settings;
            });

            services.AddSingleton<FrameCommandProcessor>();

            services.UseMicrosoftDependencyResolver();

            var container = services.BuildServiceProvider();
            container.UseMicrosoftDependencyResolver();
        }
    }
}
