using IMConsumer.Model;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace IMConsumer.Infrastructure
{
    public static class UserHelper
    {
        public static readonly string[] special_users = {"newsapp", "fmessage", "filehelper", "weibo", "qqmail",
                         "fmessage", "tmessage", "qmessage", "qqsync", "floatbottle",
                         "lbsapp", "shakeapp", "medianote", "qqfriend", "readerapp",
                         "blogapp", "facebookapp", "masssendapp", "meishiapp",
                         "feedsapp", "voip", "blogappweixin", "weixin", "brandsessionholder",
                         "weixinreminder", "wxid_novlwrv3lqwv11", "gh_22b87fa7cb3c",
                         "officialaccounts", "notification_messages", "wxid_novlwrv3lqwv11",
                         "gh_22b87fa7cb3c", "wxitil", "userexperience_alarm", "notification_messages"};

        public static ContactUser FindByUserName(this IEnumerable<ContactUser> users, string userName)
        {
            return users.FirstOrDefault(u => u.NickName == userName || u.RemarkName == userName);
        }
    }
}
