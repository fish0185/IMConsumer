using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IMConsumer.Infrastructure;
using IMConsumer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IMConsumer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            EncodingProvider provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);
            Console.OutputEncoding = Encoding.GetEncoding("GB2312");
            var host = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                    configHost.AddEnvironmentVariables(prefix: "PREFIX_");
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile(
                        $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                        optional: true);
                    configApp.AddEnvironmentVariables(prefix: "PREFIX_");
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.AddTransient<IWeChatEngine, WeChatEngine>();
                    services.AddHostedService<LifetimeEventsHostedService>();
                    services.AddHostedService<WeChatHostedService>(); 
                    services.AddHttpClient<IWeChatLoginClient, WeChatLoginHttpClient>(c =>
                    {
                        c.BaseAddress = new Uri("https://login.weixin.qq.com");
                        c.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                        c.DefaultRequestHeaders.Add("User-Agent", "HttpClientFactory-Sample");
                    });
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();

            Console.WriteLine("Host shutting down");
        }
    }
}
