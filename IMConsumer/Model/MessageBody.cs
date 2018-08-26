using System;
using System.Collections.Generic;
using System.Text;
using IMConsumer.Common;

namespace IMConsumer.Model
{
    public class MessageBody
    {
        public int Type { get; set; }

        public string Content { get; set; }

        public string FromUserName { get; set; }

        public string ToUserName { get; set; }

        private string _localId;
        public string LocalID
        {
            get {
                if (string.IsNullOrEmpty(_localId))
                {
                    var rd = new Random();
                    var a = rd.NextDouble();
                    var para2 = a.ToString("f3").Replace(".", string.Empty);
                    var para1 = DateTime.UtcNow.ToUnixTimeStamp().ToString("f0");
                    return para1 + para2;
                }
                return _localId;
            }
            set => _localId = value;
        }

        public string ClientMsgId => LocalID;

        public string MediaId { get; set; }
    }
}
