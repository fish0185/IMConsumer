using System;
using System.Collections.Generic;
using System.Text;

namespace IMConsumer.Model
{
    public class ClientLoginResponse
    {
        public string RedirectUri { get; set; }
        public string BaseUri { get; set; }
        public string BaseHost { get; set; }
    }
}
