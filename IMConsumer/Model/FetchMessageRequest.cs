using Newtonsoft.Json;

namespace IMConsumer.Model
{
    public class FetchMessageRequest
    {
        public BaseRequest BaseRequest { get; set;}

        public SyncKey SyncKey { get; set; }

        [JsonProperty("rr")]
        public long DateTimeNow { get; set; }
    }
}
