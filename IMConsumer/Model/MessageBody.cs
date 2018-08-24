using System;
using System.Collections.Generic;
using System.Text;

namespace IMConsumer.Model
{
    public class MessageBody
    {
        public int Type { get; set; }

        public string Content { get; set; }

        public string FromUserName { get; set; }

        public string ToUserName { get; set; }

        public string LocalID { get; set; }

        public string ClientMsgId { get; set; }
    }
}
