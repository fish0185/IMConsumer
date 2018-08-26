using System.Threading;
using System.Threading.Tasks;
using IMConsumer.Infrastructure;
using IMConsumer.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IMConsumer.Services
{
    public class WeChatHostedService : IHostedService
    {
        private readonly ILogger<WeChatHostedService> _logger;
        private IWeChatEngine _weChatEngine;

        public WeChatHostedService(ILogger<WeChatHostedService> logger, IWeChatEngine weChatEngine)
        {
            _logger = logger;
            _weChatEngine = weChatEngine;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service Started!");
            await _weChatEngine.Run();
            _weChatEngine.OnMessage += _weChatEngine_OnMessage;
            await _weChatEngine.SendMessage("中文测试","101");
            var result = await _weChatEngine.UploadFile(@"C:\Users\Gary\Desktop\contianerlink\2.png");
            await _weChatEngine.SendPicture("101", result.MediaId);
        }

        private void _weChatEngine_OnMessage(object sender, MessageEventArgs e)
        {
            _logger.LogInformation("you got message: " + e.MessageResponse.Content);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stopped");
            return Task.CompletedTask;
        }
    }
}
