using IMConsumer.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IMConsumer.Model
{
    public class ContactUser : User
    {
        public int MemberCount { get; set; }

        public IEnumerable<User> MemberList { get; set; }

        public int OwnerUin { get; set; }

        public int Statues { get; set; }

        public string Province { get; set; }

        public string City { get; set; }

        public string Alias { get; set; }

        public int ChatRoomId { get; set; }

        public string EncryChatRoomId { get; set; }

        public int IsOwner { get; set; }

        public ContactType ContactType { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if ((VerifyFlag & 8) != 0) //公众号
            {
                ContactType = ContactType.Public;
            }
            else if (UserHelper.special_users.Contains(UserName)) //特殊账户
            {
                ContactType = ContactType.Special;
            }
            else if (UserName.IndexOf("@@") != -1) //群聊
            {
                ContactType = ContactType.Group;
            }
            else
            {
                ContactType = ContactType.Private; //联系人
            }
        }
    }

    public enum ContactType
    {
        Unknow,
        Public, // 公众号
        Special, // 特殊账户
        Group, // 群聊
        Private // 联系人
    }
}
