using System;
using System.Collections.Generic;
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
    }
}
