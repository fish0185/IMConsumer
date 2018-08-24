using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace IMConsumer.Model
{
    [Serializable, XmlRoot("error")]
    public class WebLoginResponse
    {
        [XmlElement(ElementName = "ret")]
        public int Ret { get; set; }

        [XmlElement(ElementName = "message")]
        public string Message { get; set; }

        [XmlElement(ElementName = "skey")]
        public string Skey { get; set; }

        [XmlElement(ElementName = "wxsid")]
        public string WxSid { get; set; }

        [XmlElement(ElementName = "wxuin")]
        public long WxUin { get; set; }

        [XmlElement(ElementName = "pass_ticket")]
        public string PassTicket { get; set; }

        [XmlElement(ElementName = "isgrayscale")]
        public int IsGrayScale { get; set; }
    }
}
