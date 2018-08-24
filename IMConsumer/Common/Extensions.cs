using System.Net;

namespace IMConsumer.Common
{
    public static class Extensions
    {
        public static string UrlDecode(this string text)
        {
            return WebUtility.UrlDecode(text);
        }
    }
}
