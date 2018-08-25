using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace IMConsumer.Services
{
    public interface IWeChatMessageClient
    {
        Task<string> Post(string url, string text);

        Task<string> Get(string url);
    }

    public class WeChatMessageClient : IWeChatMessageClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeChatMessageClient> _logger;

        public WeChatMessageClient(HttpClient httpClient, ILoggerFactory loggerFactory)
        {
            _httpClient = httpClient;
            _logger = loggerFactory.CreateLogger<WeChatMessageClient>();
        }

        public async Task<string> Get(string url)
        {
            _logger.LogInformation($"Get: {url}");
            string strResult = "";
            try
            {
                using (var result = await _httpClient.GetStreamAsync(url))
                using (var streamReader = new StreamReader(result, Encoding.UTF8))
                {
                    var content = await streamReader.ReadToEndAsync();
                    return content;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get error: {url}");
            }
            return strResult;
        }

        public async Task<string> Post(string url, string text)
        {
            _logger.LogInformation($"Post: {url} - {text}");
            string strResult = "";
            try
            {
                byte[] bs = Encoding.UTF8.GetBytes(text);
                var content = new ByteArrayContent(bs);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                using (var res = await _httpClient.PostAsync(url, content))
                {
                    var result = await res.Content.ReadAsStringAsync();
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Post error: {url}");
            }
            return strResult;
        }
    }
}
