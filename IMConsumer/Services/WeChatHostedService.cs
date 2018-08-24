using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IMConsumer.Common;
using IMConsumer.Infrastructure;
using IMConsumer.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;

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
            await _weChatEngine.SendMessage(new Message());
        }

        private void _weChatEngine_OnMessage(object sender, EventArgs e)
        {
            _logger.LogInformation("you got meesage");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stoped");
            return Task.CompletedTask;
        }
    }
}
