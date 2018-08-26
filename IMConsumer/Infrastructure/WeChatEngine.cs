using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IMConsumer.Common;
using IMConsumer.Model;
using IMConsumer.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using System.Diagnostics;

namespace IMConsumer.Infrastructure
{
    public interface IWeChatEngine
    {
        event EventHandler<MessageEventArgs> OnMessage;

        Task Run();

        Task<bool> SendMessage(string message, string userName);

        Task<MediaUploadResponse> UploadFile(string filePath);

        Task<bool> SendPicture(string userName, string mediaId);

        bool IsReady { get; }
    }

    public class WeChatEngine : IWeChatEngine
    {
        public event EventHandler<MessageEventArgs> OnMessage;

        private readonly ILogger<WeChatEngine> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IWeChatLoginClient _weChatClient;
        private string _deviceId = "e" + new Random().NextDouble().ToString("f16").Replace(".", string.Empty).Substring(1);     // 'e' + repr(random.random())[2:17]
        private IWeChatMessageClient _weChatMessageClient;
        private WeChatInitResponse _weChatInitResponse;
        private WebLoginResponse _webLoginResponse;
        private ClientLoginResponse _clientLoginResponse;
        private string _syncKey = string.Empty;
        private GetContactResponse _contactsResponse;

        public bool IsReady { get; set; }

        public WeChatEngine(
            ILogger<WeChatEngine> logger,
            ILoggerFactory loggerFactory,
            IWeChatLoginClient weChatClient)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _weChatClient = weChatClient;
        }

        public async Task Run()
        {
            _logger.LogInformation("WeChat Engine Starting.....");

            var uuid = await _weChatClient.GetUuid();
            GenerateQR(UrlEndpoints.QRCode + uuid);

            _clientLoginResponse = await WaitForLogin(uuid);

            _webLoginResponse = await Login(_clientLoginResponse);

            _weChatInitResponse = await InitApp(_webLoginResponse, _clientLoginResponse);

            await GetContacts();

            SyncMessage();
            IsReady = true;
            _logger.LogInformation("WeChat Engine Started.....");
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
            await TestSyncCheck();
            while (true)
            {
                var check_time = DateTime.UtcNow.ToDouleUnixTimeStamp();
                try
                {
                    // fire a get request to wechat server
                    // reponse will be telling you whether there is message/event
                    string[] syncCheckResult = await SyncCheck();
                    string retcode = syncCheckResult[0];
                    string selector = syncCheckResult[1];

                    if (retcode == "1100")
                    {
                        _logger.LogWarning("从微信客户端上登出");
                        break;
                    }
                    else if (retcode == "1101")
                    {
                        _logger.LogWarning("从其它设备上登了网页微信");
                        break;
                    }
                    else if (retcode == "0")
                    {
                        if (selector == "2")  // 有新消息
                        {
                            JObject result = await FetchMessage();
                            if (result != null)
                            {
                                foreach (var msg in result["AddMsgList"].ToObject<List<MessageResponse>>().Where(msg => msg.MsgType != 51))
                                {
                                    OnMessage?.Invoke(this, new MessageEventArgs(msg));
                                }
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
                            JObject r = await FetchMessage();
                            if (r != null)
                            {
                                //get_contact();
                                _logger.LogInformation("[INFO] Contacts Updated .");
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
                    _logger.LogError(ex, "Exception in Polling");
                }

                check_time = DateTime.UtcNow.ToDouleUnixTimeStamp() - check_time;
                var sleepTime = 5000 - check_time;
                if (sleepTime > 0)
                {
                    await Task.Delay((int)sleepTime);
                }
            }
        }

        private async Task<JObject> FetchMessage()
        {
            var fetchRequest = new FetchMessageRequest
            {
                BaseRequest = new BaseRequest
                {
                    DeviceID = _deviceId,
                    Sid = _webLoginResponse.WxSid,
                    Skey = _webLoginResponse.Skey,
                    Uin = _webLoginResponse.WxUin.ToString()
                },
                SyncKey = _weChatInitResponse.SyncKey,
                DateTimeNow = DateTime.UtcNow.ToUnixTimeStamp()
            };

            var fetchRequestJson = JsonConvert.SerializeObject(fetchRequest);

            if (_webLoginResponse.WxSid == null)
            {
                return null;
            }

            var fetchMessageEndpoint = string.Format(
                UrlEndpoints.FetchMessage,
                _clientLoginResponse.BaseUri,
                _webLoginResponse.WxSid,
                _webLoginResponse.Skey,
                _webLoginResponse.PassTicket);

            var messageStr = await _weChatMessageClient.Post(fetchMessageEndpoint, fetchRequestJson);
            var messageResult = JsonConvert.DeserializeObject(messageStr) as JObject;
            var newSyncKey = messageResult["SyncKey"].ToObject<SyncKey>();
            _weChatInitResponse.SyncKey = newSyncKey;
            if (newSyncKey.Count > 0)
            {
                _syncKey = newSyncKey.ToString();
            }

            return messageResult;
        }

        private BaseRequest MapperToBaseRequest(WebLoginResponse webLoginResponse) => new BaseRequest
        {
            Uin = webLoginResponse.WxUin.ToString(),
            Sid = webLoginResponse.WxSid,
            Skey = webLoginResponse.Skey,
            DeviceID = _deviceId
        };

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
                _syncKey += (syncKey.Key + "_" + syncKey.Val + "%7C");
            }
            _syncKey = _syncKey.TrimEnd('%', '7', 'C');

            return init;
        }

        private async Task GetContacts()
        {
            var contactsUrl = string.Format(UrlEndpoints.Contacts,
                _clientLoginResponse.BaseUri,
                _webLoginResponse.PassTicket,
                _webLoginResponse.Skey,
                Utility.ConvertDateTimeToInt(DateTime.Now));

            var result = await _weChatMessageClient.Get(contactsUrl);

            _contactsResponse = JsonConvert.DeserializeObject<GetContactResponse>(result);
        }

        private async Task<WebLoginResponse> Login(ClientLoginResponse response)
        {
            if (response.RedirectUri.Length < 4)
            {
                _logger.LogError("[ERROR] Login failed due to network problem, please try again.");
                return null;
            }

            var cookie = new CookieContainer();
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookie
            };
            var http = new HttpClient(handler);
            var serverRes = await http.GetAsync(response.RedirectUri);
            var result = await serverRes.Content.ReadAsStringAsync();
            _weChatMessageClient = new WeChatMessageClient(http, _loggerFactory);

            return Utility.XmlDeserialize<WebLoginResponse>(result, rep => rep.PassTicket = rep.PassTicket.UrlDecode());
        }

        private void GenerateQR(string url)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);
            qrCodeImage.Save("QRcode.png");
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(@"C:\Windows\System32\mspaint.exe")
            {
                UseShellExecute = true,
                Arguments = "QRcode.png"
            };
            p.Start();
        }

        private async Task<ClientLoginResponse> WaitForLogin(string uuid)
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
                var loginCheckUrl = string.Format(UrlEndpoints.LoginCheck, tip, uuid, Utility.ConvertDateTimeToInt(DateTime.Now));
                var login_result = await _weChatClient.Get(loginCheckUrl);
                Match match = Regex.Match(login_result, "window.code=(\\d+)");
                if (match.Success)
                {
                    status_data = login_result;
                    status_code = match.Groups[1].Value;
                }

                if (status_code == "201") //已扫描 未登录
                {
                    _logger.LogInformation("Please confirm to login.");
                    tip = "0";
                }
                else if (status_code == "200")  //已扫描 已登录
                {
                    _logger.LogInformation("Login Success!");
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
                    _logger.LogWarning("[ERROR] WeChat login exception return_code=" + status_code + ". retry in" + try_later_secs + "secs later...");
                    tip = "1";
                    retry_time -= 1;
                    await Task.Delay(try_later_secs * 1000);
                }

                await Task.Delay(800);
            }

            return null;
        }

        public async Task<bool> SendMessage(string message, string userName)
        {
            _logger.LogInformation("Sending Message...");
            var user = _contactsResponse.MemberList.FindByUserName(userName);
            if (user == null)
            {
                _logger.LogWarning($"User: {userName} not exist");
                return false;
            }

            var messageEndpoint = string.Format(
                UrlEndpoints.SendTextMessage,
                _clientLoginResponse.BaseUri,
                _webLoginResponse.PassTicket);

            var msg = new Message
            {
                BaseRequest = MapperToBaseRequest(_webLoginResponse),
                Msg = new MessageBody
                {
                    Type = 1,
                    FromUserName = _weChatInitResponse.User.UserName,
                    ToUserName = user.UserName,
                    Content = Utility.ConvertGB2312ToUTF8(message)
                }
            };

            await _weChatMessageClient.Post(messageEndpoint, JsonConvert.SerializeObject(msg));

            return true;
        }

        /// <summary>
        /// Need to resovle sync Url from here
        /// </summary>
        /// <returns></returns>
        private async Task<bool> TestSyncCheck()
        {
            try
            {
                if((await SyncCheck())[0] == "0")
                {
                    return true;
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed on TestSyncCheck");
            }

            return false;
            //sync_host = "webpush." + base_host;
            //sync_host = "webpush2." + base_host;

            //try
            //{
            //    retcode = (await SyncCheck())[0];
            //}
            //catch
            //{
            //    retcode = "-1";
            //}
            //if (retcode == "0") return true;
            //return false;
        }

        private async Task<string[]> SyncCheck()
        {
            try
            {
                var syncCheckEndpoint = string.Format(UrlEndpoints.SyncCheck,
                    UrlEndpoints.SyncHost,
                    _webLoginResponse.WxSid,
                    _webLoginResponse.WxUin,
                    _syncKey,
                    DateTime.UtcNow.ToUnixTimeStamp(),
                    _webLoginResponse.Skey.Replace("@", "%40"),
                    _deviceId,
                    Utility.ConvertDateTimeToInt(DateTime.Now));
                var retcode = "";
                var selector = "";
                var ReturnValue = await _weChatMessageClient.Get(syncCheckEndpoint);
                var match = Regex.Match(ReturnValue, "window.synccheck=\\{retcode:\"(\\d+)\",selector:\"(\\d+)\"\\}");
                if (match.Success)
                {
                    retcode = match.Groups[1].Value;
                    selector = match.Groups[2].Value;
                }
                return new [] { retcode, selector };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "SyncCheck Failed!");
                return new [] { "-1", "-1" };
            }
        }

        public Task<MediaUploadResponse> UploadFile(string filePath)
        {
            MediaUploadRequest request = new MediaUploadRequest
            {
                BaseRequest = MapperToBaseRequest(_webLoginResponse),
                ClientMediaId = DateTime.UtcNow.ToUnixTimeInSeconds(),
                StartPos = 0,
                MediaType = 4
            };

            return _weChatMessageClient.Upload(_webLoginResponse.PassTicket, filePath, request);
        }

        public async Task<bool> SendPicture(string userName, string mediaId)
        {
            var user = _contactsResponse.MemberList.FindByUserName(userName);
            if (user == null)
            {
                _logger.LogWarning($"User: {userName} not exist");
                return false;
            }

            var url = string.Format(UrlEndpoints.SendPicture, _clientLoginResponse.BaseUri, _webLoginResponse.PassTicket);

            var msg = new Message
            {
                BaseRequest = MapperToBaseRequest(_webLoginResponse),
                Msg = new MessageBody
                {
                    Type = 3,
                    FromUserName = _weChatInitResponse.User.UserName,
                    ToUserName = user.UserName,
                    Content = "",
                    MediaId = mediaId
                }
            };

            await _weChatMessageClient.PostJson(url, msg);

            return true;
        }
    }
}
