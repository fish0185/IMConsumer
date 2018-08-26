namespace IMConsumer.Model
{
    public class MediaUploadRequest
    {
        public long ClientMediaId { get; set; }
        public int MediaType { get; set; }
        public string DataLen { get; set; }
        public string TotalLen { get; set; }
        public int StartPos { get; set; }
        public BaseRequest BaseRequest { get; set; }
    }
}
