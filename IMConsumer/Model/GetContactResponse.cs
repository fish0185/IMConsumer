using System;
using System.Collections.Generic;
using System.Text;

namespace IMConsumer.Model
{
    public class GetContactResponse
    {
        public BaseResponse BaseResponse { get; set; }

        public int MemberCount { get; set; }

        public int Seq { get; set; }

        public IEnumerable<ContactUser> MemberList { get; set; }
    }
}
