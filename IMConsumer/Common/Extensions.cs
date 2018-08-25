using System;
using System.Net;

namespace IMConsumer.Common
{
    public static class Extensions
    {
        public static string UrlDecode(this string text)
        {
            return WebUtility.UrlDecode(text);
        }

        public static long ToUnixTimeStamp(this DateTime datetime)
        {
            return (long)(datetime - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        public static double ToDouleUnixTimeStamp(this DateTime datetime)
        {
            return (datetime - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}
