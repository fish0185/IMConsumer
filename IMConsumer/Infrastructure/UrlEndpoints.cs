namespace IMConsumer.Infrastructure
{
    public class UrlEndpoints
    {
        public const string FetchMessage = "{0}/webwxsync?sid={1}&lang=zh_CN&skey={2}&pass_ticket={3}";

        public const string SyncHost = "web.wechat.com";

        public const string LoginCheck = "https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip={0}&uuid={1}&_={2}";

        public const string QRCode = "https://login.weixin.qq.com/l/";

        public const string Contacts = "{0}/webwxgetcontact?pass_ticket={1}&skey={2}&r={3}";

        public const string SyncCheck = "https://{0}/cgi-bin/mmwebwx-bin/synccheck?sid={1}&uin={2}&synckey={3}&r={4}&skey={5}&deviceid={6}&_={7}";
    }
}
