using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IMConsumer.Common;
using IMConsumer.Model;
using IMConsumer.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;

namespace IMConsumer.Infrastructure
{
    public interface IWeChatEngine
    {
        event EventHandler<EventArgs> OnMessage;

        Task Run();

        Task SendMessage(Message message);
    }

    public class WeChatEngine : IWeChatEngine
    {
        public event EventHandler<EventArgs> OnMessage;

        private readonly ILogger<WeChatEngine> _logger;
        private readonly IWeChatLoginClient _weChatClient;
        private string _deviceId = "e" + new Random().NextDouble().ToString("f16").Replace(".", string.Empty).Substring(1);     // 'e' + repr(random.random())[2:17]

        private IWeChatMessageClient _weChatMessageClient;
        private WeChatInitResponse weChatInitResponse;
        private WebLoginResponse webLoginResponse;
        private ClientLoginResponse clientLoginResponse;
        private string sync_key_str = string.Empty;

        public WeChatEngine(ILogger<WeChatEngine> logger, IWeChatLoginClient weChatClient)
        {
            _logger = logger;
            _weChatClient = weChatClient;
        }

        public async Task Run()
        {
            _logger.LogInformation("Service started");

            var uuid = await _weChatClient.GetUuid();
            GenerateQR("https://login.weixin.qq.com/l/" + uuid);

            clientLoginResponse = await WaitForLogin(uuid);

            webLoginResponse = await Login(clientLoginResponse);

            weChatInitResponse = await InitApp(webLoginResponse, clientLoginResponse);

            SyncMessage();
        }

        private void SyncMessage()
        {
            Task.Factory.StartNew(async () =>
            {
                await Polling();
            }, TaskCreationOptions.LongRunning);
        }

        private async Task Polling()
        {
            await test_sync_check();
            while (true)
            {
                float check_time = (float)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds;
                try
                {
                    string[] ReturnArray = await sync_check();//[retcode, selector] 
                    string retcode = ReturnArray[0];
                    string selector = ReturnArray[1];

                    if (retcode == "1100")  //从微信客户端上登出
                        break;
                    else if (retcode == "1101") // 从其它设备上登了网页微信
                        break;
                    else if (retcode == "0")
                    {
                        if (selector == "2")  // 有新消息
                        {
                            JObject r = await sync();
                            if (r != null)
                            {
                                //handle_msg(r);
                                Console.WriteLine("has message");
                            }
                        }
                        //else if ( selector == "3")  // 未知
                        //{
                        //    r = self.sync()
                        //    if r is not None:
                        //        self.handle_msg(r)
                        //}
                        else if (selector == "4")   //通讯录更新
                        {
                            JObject r = await sync();
                            if (r != null)
                            {
                                //get_contact();
                                Console.WriteLine("[INFO] Contacts Updated .");
                            }
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        await Task.Delay(2000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ERROR] Except in proc_msg");
                    Console.WriteLine(ex.ToString());
                }
                check_time = (float)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds - check_time;
                if (check_time < 0.8)
                    await Task.Delay((int)(1.0 - check_time) * 1000);
            }
        }

        public async Task<JObject> sync()
        {
            string sync_json = "{{\"BaseRequest\" : {{\"DeviceID\":\"{6}\",\"Sid\":\"{1}\", \"Skey\":\"{5}\", \"Uin\":\"{0}\"}},\"SyncKey\" : {{\"Count\":{2},\"List\":[{3}]}},\"rr\" :{4}}}";
            string sync_keys = "";
            foreach (var p in weChatInitResponse.SyncKey.List)
            {
                sync_keys += "{\"Key\":" + p.Key + ",\"Val\":" + p.Val + "},";
            }
            sync_keys = sync_keys.TrimEnd(',');
            sync_json = string.Format(sync_json, webLoginResponse.WxUin, webLoginResponse.WxSid, weChatInitResponse.SyncKey.List.Count(), sync_keys, (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds, webLoginResponse.Skey, _deviceId);

            if (webLoginResponse.WxSid != null)
            {
                string sync_str = await _weChatMessageClient.Post(clientLoginResponse.BaseUri + "/webwxsync?sid=" + webLoginResponse.WxSid + "&lang=zh_CN&skey=" + webLoginResponse.Skey + "&pass_ticket=" + webLoginResponse.PassTicket, sync_json);


                JObject sync_resul = JsonConvert.DeserializeObject(sync_str) as JObject;

                if (sync_resul["SyncKey"]["Count"].ToString() != "0")
                {
                    Dictionary<string, string> dic_sync_key_temp = new Dictionary<string, string>();
                    foreach (JObject key in sync_resul["SyncKey"]["List"])
                    {
                        dic_sync_key_temp.Add(key["Key"].ToString(), key["Val"].ToString());
                    }
                    sync_key_str = "";
                    foreach (KeyValuePair<string, string> p in dic_sync_key_temp)
                    {
                        sync_key_str += p.Key + "_" + p.Value + "%7C";
                    }
                    sync_key_str = sync_key_str.TrimEnd('%', '7', 'C');
                }
                return sync_resul;
            }
            else
            {
                return null;
            }
        }

        private BaseRequest MapperToBaseRequest(WebLoginResponse webLoginResponse)
        {
            var br = new BaseRequest
            {
                Uin = webLoginResponse.WxUin.ToString(),
                Sid = webLoginResponse.WxSid,
                Skey = webLoginResponse.Skey,
                DeviceID = _deviceId
            };

            return br;
            //var base_request = "{{\"BaseRequest\":{{\"Uin\":\"{0}\",\"Sid\":\"{1}\",\"Skey\":\"{2}\",\"DeviceID\":\"{3}\"}}}}";
            //return string.Format(base_request, webLoginResponse.WxUin, webLoginResponse.WxSid, webLoginResponse.Skey, _deviceId);
        }

        private async Task<WeChatInitResponse> InitApp(WebLoginResponse webLoginResponse, ClientLoginResponse clientLoginResponse)
        {
            var base_request = MapperToBaseRequest(webLoginResponse);
            var body = new
            {
                BaseRequest = base_request
            };

            string initInfo = await _weChatMessageClient.Post(clientLoginResponse.BaseUri + "/webwxinit?r=" + Utility.ConvertDateTimeToInt(DateTime.Now) + "&lang=en_US" + "&pass_ticket=" + webLoginResponse.PassTicket, JsonConvert.SerializeObject(body));

            var init = JsonConvert.DeserializeObject<WeChatInitResponse>(initInfo);

            foreach (var syncKey in init.SyncKey.List)
            {
                sync_key_str += (syncKey.Key.ToString() + "_" + syncKey.Val + "%7C");
            }
            sync_key_str = sync_key_str.TrimEnd('%', '7', 'C');

            return init;

            //foreach (JObject synckey in init_result["SyncKey"]["List"])  //同步键值
            //{
            //    dic_sync_key.Add(synckey["Key"].ToString(), synckey["Val"].ToString());
            //}
            //foreach (KeyValuePair<string, string> p in dic_sync_key)
            //{
            //    sync_key_str += p.Key + "_" + p.Value + "%7C";
            //}
            //sync_key_str = sync_key_str.TrimEnd('%', '7', 'C');
            //return init_result["BaseResponse"]["Ret"].ToString() == "0";
        }

        private async Task<WebLoginResponse> Login(ClientLoginResponse response)
        {
            if (response.RedirectUri.Length < 4)
            {
                Console.WriteLine("[ERROR] Login failed due to network problem, please try again.");
                return null;
            }
            CookieContainer cookie = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookie
            };
            var http = new HttpClient(handler);
            var serverRes = await http.GetAsync(response.RedirectUri);
            var result = await serverRes.Content.ReadAsStringAsync();
            _weChatMessageClient = new WeChatMessageClient(http);

            return Utility.XmlDeserialize<WebLoginResponse>(result, rep => rep.PassTicket = rep.PassTicket.UrlDecode());
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
                string login_result = await _weChatClient.Get("https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?" + "tip=" + tip + "&uuid=" + uuid + "&_=" + Utility.ConvertDateTimeToInt(DateTime.Now));
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

        public Task SendMessage(Message message)
        {
            _logger.LogInformation("gooooood");
            return Task.CompletedTask;
        }

        string sync_host = "web.wechat.com";

        public async Task<bool> test_sync_check()
        {
            string retcode = "";
            //sync_host = base_host;
            //sync_host = "web.wechat.com";
            try
            {
                retcode = (await sync_check())[0];
            }
            catch
            {
                retcode = "-1";
            }
            if (retcode == "0") return true;
            //sync_host = "webpush." + base_host;
            //sync_host = "webpush2." + base_host;

            try
            {
                retcode = (await sync_check())[0];
            }
            catch
            {
                retcode = "-1";
            }
            if (retcode == "0") return true;
            return false;
        }

        public async Task<string[]> sync_check()
        {
            string retcode = "";
            string selector = "";

            string _synccheck_url = "https://{0}/cgi-bin/mmwebwx-bin/synccheck?sid={1}&uin={2}&synckey={3}&r={4}&skey={5}&deviceid={6}&_={7}";
            _synccheck_url = string.Format(_synccheck_url, sync_host, webLoginResponse.WxSid, webLoginResponse.WxUin, sync_key_str, (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds, webLoginResponse.Skey.Replace("@", "%40"), _deviceId, Utility.ConvertDateTimeToInt(DateTime.Now));
            try
            {
                string ReturnValue = await _weChatMessageClient.Get(_synccheck_url);
                Match match = Regex.Match(ReturnValue, "window.synccheck=\\{retcode:\"(\\d+)\",selector:\"(\\d+)\"\\}");
                if (match.Success)
                {
                    retcode = match.Groups[1].Value;
                    selector = match.Groups[2].Value;
                }
                return new string[2] { retcode, selector };

            }
            catch
            {
                return new string[2] { "-1", "-1" };
            }
        }
    }
}
