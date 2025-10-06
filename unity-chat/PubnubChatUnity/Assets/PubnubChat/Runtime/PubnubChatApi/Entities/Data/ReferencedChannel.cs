using Newtonsoft.Json;

namespace PubnubChatApi
{
    public struct ReferencedChannel
    {
        [JsonProperty("id")]
        public string Id;
        [JsonProperty("name")]
        public string Name;
    }
}