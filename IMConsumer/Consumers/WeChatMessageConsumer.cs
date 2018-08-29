using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IMConsumer.Infrastructure;
using IMConsumer.Model;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace IMConsumer.Consumers
{
    public class WeChatMessageConsumer : IConsumer<MessageResponse>
    {
        private readonly ILogger<WeChatMessageConsumer> _logger;
        private readonly IWeChatEngine _weChatEngine;

        public WeChatMessageConsumer(
            ILogger<WeChatMessageConsumer> logger,
            IWeChatEngine weChatEngine)
        {
            _logger = logger;
            _weChatEngine = weChatEngine;
        }

        public Task Consume(ConsumeContext<MessageResponse> context)
        {
            _logger.LogInformation("Consuming message from: " + context.Message.FromUserName + " - " + context.Message.Content);
            if (_weChatEngine.IsReady)
            {
                // start Ai
                _weChatEngine.SendMessage(context.Message.Content, "101");

                return Task.CompletedTask;
            }

            throw new InvalidOperationException("Please make sure WeChat is login!");
        }
    }
}
