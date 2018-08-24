using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IMConsumer.Common;

namespace IMConsumer.Services
{
    public interface IWeChatLoginClient
    {
        Task<string> GetUuid();
        Task<string> Get(string url);
    }

    public class WeChatLoginHttpClient : IWeChatLoginClient
    {
        private readonly HttpClient _httpClient; // not exposed publicly

        public WeChatLoginHttpClient(HttpClient client)
        {
            _httpClient = client;
        }

        public async Task<string> GetUuid()
        {
            var response = await _httpClient.GetAsync("jslogin?appid=wx782c26e4c19acffb&fun=new&lang=zh_CN&_=" +
                                 Utility.ConvertDateTimeToInt(DateTime.Now));
            var result = await response.Content.ReadAsStringAsync();

            Match match = Regex.Match(result, "window.QRLogin.code = (\\d+); window.QRLogin.uuid = \"(\\S+?)\"");
            if (match.Success && "200" == match.Groups[1].Value)
            {
                return match.Groups[2].Value;
            }

            return string.Empty;
        }

        public async Task<string> Get(string url)
        {
            var response = await _httpClient.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
    }
}
