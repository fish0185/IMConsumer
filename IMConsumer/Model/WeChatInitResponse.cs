using System;
using System.Collections.Generic;
using System.Text;

namespace IMConsumer.Model
{
    public class WeChatInitResponse
    {
        public BaseResponse BaseResponse { get; set; }

        public int Count { get; set; }

        public IEnumerable<ContactUser> ContactList { get; set; }

        public SyncKey SyncKey { get; set; }

        public User User { get; set; }

        public string ChatSet { get; set; }

        public string SKey { get; set; }

        public int ClientVersion { get; set; }

        public long SystemTime { get; set; }

        public int GrayScale { get; set; }

        public int InviteStartCount { get; set; }

        public int MPSubscribeMsgCount { get; set; }

        public int ClickReportInterval { get; set; }
    }
}
