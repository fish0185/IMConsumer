using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GreenPipes;
using IMConsumer.Consumers;
using IMConsumer.Infrastructure;
using IMConsumer.Model;
using IMConsumer.Options;
using IMConsumer.Services;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
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
            EncodingProvider encodingProvider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(encodingProvider);
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
                    var rabbitMqConfig = hostContext.Configuration.GetSection("RabbitMq").Get<RabbitMqConfig>();
                    services.AddLogging();
                    services.AddSingleton<IWeChatEngine, WeChatEngine>();
                    services.AddHostedService<LifetimeEventsHostedService>();
                    services.AddHostedService<WeChatHostedService>();
                    services.AddHttpClient<IWeChatLoginClient, WeChatLoginHttpClient>(c =>
                    {
                        c.BaseAddress = new Uri("https://login.weixin.qq.com");
                        c.DefaultRequestHeaders.Add("User-Agent", ConstValues.UserAgent);
                    });
                    services.AddScoped<WeChatMessageConsumer>();
                    services.AddMassTransit(x =>
                    {
                        // add the consumer, for LoadFrom
                        x.AddConsumer<WeChatMessageConsumer>();
                    });
                    services.AddSingleton( provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                    {
                        var rabbitQHost = cfg.Host(new Uri(rabbitMqConfig.Endpoint),  "/", h =>
                        {
                            h.Username(rabbitMqConfig.UserName);
                            h.Password(rabbitMqConfig.Password);
                        });

                        cfg.ReceiveEndpoint(rabbitQHost, "wechat-message", e =>
                        {
                            e.PrefetchCount = 16;
                            e.UseMessageRetry(x => x.Interval(2, 100));

                            e.LoadFrom(provider);

                            EndpointConvention.Map<MessageResponse>(e.InputAddress);
                        });
                    }));

                    services.AddSingleton<IPublishEndpoint>(provider => provider.GetRequiredService<IBusControl>());
                    services.AddSingleton<ISendEndpointProvider>(provider => provider.GetRequiredService<IBusControl>());
                    services.AddSingleton<IBus>(provider => provider.GetRequiredService<IBusControl>());
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
