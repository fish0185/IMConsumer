using System;
using System.Collections.Generic;
using System.Text;

namespace IMConsumer.Model
{
    public class Message
    {
        public MessageBody Msg { get; set; }

        public BaseRequest BaseRequest { get; set; }
    }
}
