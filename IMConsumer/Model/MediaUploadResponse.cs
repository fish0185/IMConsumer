namespace IMConsumer.Model
{
    public class MediaUploadResponse
    {
        public BaseResponse BaseResponse { get; set; }
        public string MediaId { get; set; }
        public int StartPos { get; set; }
        public int CDNThumbImgHeight { get; set; }
        public int CDNThumbImgWeight { get; set; }
    }
}
