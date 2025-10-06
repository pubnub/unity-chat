using Newtonsoft.Json;

namespace PubnubChatApi
{
    public class TextLink
    {
        [JsonProperty("start_index")]
        public int StartIndex;
        [JsonProperty("end_index")]
        public int  EndIndex;
        [JsonProperty("link")]
        public string Link = string.Empty;
    }
}