using System;
using System.Collections.Generic;
using System.Text;

namespace IMConsumer.Model
{
    public class BaseUser
    {
        public int Uin { get; set; }

        public string UserName { get; set; }

        public string NickName { get; set; }

        public int AttrStatus { get; set; }

        public string PYInitial { get; set; }

        public string PYQuanPin { get; set; }

        public string RemarkPYInitial { get; set; }

        public int MemberStatus { get; set; }

        public string DisplayName { get; set; }

        public string KeyWord { get; set; }
    }
}
