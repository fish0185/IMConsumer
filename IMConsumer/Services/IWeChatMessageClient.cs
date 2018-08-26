using IMConsumer.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using IMConsumer.Infrastructure;

namespace IMConsumer.Services
{
    public interface IWeChatMessageClient
    {
        Task<string> Post(string url, string text);

        Task<string> Get(string url);

        Task<MediaUploadResponse> Upload(string passTicket, string filePath, MediaUploadRequest mediaUploadRequest);

        Task<string> PostJson(string url, Message message);
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

        public async Task<string> PostJson(string url, Message message)
        {
            var text = JsonConvert.SerializeObject(message);
            _logger.LogInformation($"PostJson: {url} - {text}");
            string strResult = "";
            try
            {
                byte[] bs = Encoding.UTF8.GetBytes(text);
                var content = new ByteArrayContent(bs);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
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

        public async Task<MediaUploadResponse> Upload(string passTicket, string filePath, MediaUploadRequest mediaUploadRequest)
        {
            var text = JsonConvert.SerializeObject(mediaUploadRequest);
            _logger.LogInformation($"Upload: {UrlEndpoints.FileUpload} - {text}");
            var restClient = new RestClient(UrlEndpoints.FileUpload)
            {
                UserAgent = ConstValues.UserAgent
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "multipart/form-data");
            request.AddFile("filename", filePath);
            request.AddParameter("uploadmediarequest", JsonConvert.SerializeObject(mediaUploadRequest));
            request.AddParameter("pass_ticket", passTicket);

            var response = await restClient.ExecutePostTaskAsync(request);
            return JsonConvert.DeserializeObject<MediaUploadResponse>(response.Content);
        }
    }
}
