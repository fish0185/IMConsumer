namespace IMConsumer.Model
{
    public class User : BaseUser
    {
        public string HeadImgUrl { get; set; }

        public string RemarkName { get; set; }

        public string RemarkPYQuanPin { get; set; }

        public int HideInputBarFlag { get; set; }

        public int StarFriend { get; set; }

        public int Sex { get; set; }

        public string Signature { get; set; }

        public int AppAccountFlag { get; set; }

        public int VerifyFlag { get; set; }

        public int SnsFlag { get; set; }

        public int ContactFlag { get; set; }

        public int WebWxPluginSwitch { get; set; }

        public int HeadImgFlag { get; set; }
    }
}
