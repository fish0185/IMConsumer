using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IMConsumer.Common;
using IMConsumer.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QRCoder;

namespace IMConsumer.Services
{
    public class WeChatHostedService : IHostedService
    {
        private readonly ILogger<WeChatHostedService> _logger;
        private readonly IWeChatClient _weChatClient;

        public WeChatHostedService(ILogger<WeChatHostedService> logger, IWeChatClient weChatClient)
        {
            _logger = logger;
            _weChatClient = weChatClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service started");
            var re = Utility.Deserialize<WebLoginResponse>(@"<error>
<ret>1</ret>
<message></message>
<gar></gar>
</error>");

            //var uuid = await _weChatClient.GetUuid();
            //GenerateQR("https://login.weixin.qq.com/l/" + uuid);

            //var loginResponse = await WaitForLogin(uuid);

            //var isLogin = await Login(loginResponse);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stoped");
            return Task.CompletedTask;
        }

        public async Task<bool> Login(ClientLoginResponse response)
        {
            if (response.RedirectUri.Length < 4)
            {
                Console.WriteLine("[ERROR] Login failed due to network problem, please try again.");
                return false;
            }
            CookieContainer cookie = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookie
            };
            var http = new HttpClient(handler);
            var serverRes = await http.GetAsync(response.RedirectUri);
            var result =  await serverRes.Content.ReadAsStringAsync();
            var re = Utility.Deserialize<WebLoginResponse>(result);
            //string SessionInfo = WebRequestMethods.Http.WebGet(response.RedirectUri);
            //pass_ticket = SessionInfo.Split(new string[] { "pass_ticket" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');
            //skey = SessionInfo.Split(new string[] { "skey" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');
            //sid = SessionInfo.Split(new string[] { "wxsid" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');
            //uin = SessionInfo.Split(new string[] { "wxuin" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');
            //if (pass_ticket == "" || skey == "" | sid == "" | uin == "")
            //{
            //    return false;
            //}
            //base_request = "{{\"BaseRequest\":{{\"Uin\":\"{0}\",\"Sid\":\"{1}\",\"Skey\":\"{2}\",\"DeviceID\":\"{3}\"}}}}";
            //base_request = string.Format(base_request, uin, sid, skey, device_id);
            return true;
        }

        private void GenerateQR(string url)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            qrCodeImage.Save("QR.png");
        }

        public async Task<ClientLoginResponse> WaitForLogin(string uuid)
        {
            //     http comet:
            //tip=1, 等待用户扫描二维码,
            //       201: scaned
            //       408: timeout
            //tip=0, 等待用户确认登录,
            //       200: confirmed
            string tip = "1";
            int try_later_secs = 1;
            int MAX_RETRY_TIMES = 10;
            string code = "unknown";
            int retry_time = MAX_RETRY_TIMES;
            string status_code = null;
            string status_data = null;
            while (retry_time > 0)
            {
                string login_result = await _weChatClient.Get("https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?" + "tip=" + tip + "&uuid=" + uuid + "&_=" + Utility.ConvertDateTimeToInt(DateTime.Now)) ;
                Match match = Regex.Match(login_result, "window.code=(\\d+)");
                if (match.Success)
                {
                    status_data = login_result;
                    status_code = match.Groups[1].Value;
                }

                if (status_code == "201") //已扫描 未登录
                {
                    Console.WriteLine("[INFO] Please confirm to login .");
                    tip = "0";
                }
                else if (status_code == "200")  //已扫描 已登录
                {
                    match = Regex.Match(status_data, "window.redirect_uri=\"(\\S+?)\"");
                    if (match.Success)
                    {
                        var redirect_uri = match.Groups[1].Value + "&fun=new";
                        var base_uri = redirect_uri.Substring(0, redirect_uri.LastIndexOf('/'));
                        var temp_host = base_uri.Substring(8);

                        return new ClientLoginResponse
                        {
                            RedirectUri = redirect_uri,
                            BaseUri = base_uri,
                            BaseHost = temp_host
                        };
                    }
                }
                else if (status_code == "408")  //超时
                {
                    Console.WriteLine("[ERROR] WeChat login exception return_code=" + status_code + ". retry in" + try_later_secs + "secs later...");
                    tip = "1";
                    retry_time -= 1;
                    await Task.Delay(try_later_secs * 1000);
                }

                await Task.Delay(800);
            }

            return null;
        }
    }
}
