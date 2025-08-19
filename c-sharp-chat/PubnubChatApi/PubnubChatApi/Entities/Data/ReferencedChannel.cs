using Newtonsoft.Json;

namespace PubnubChatApi.Entities.Data
{
    public struct ReferencedChannel
    {
        [JsonProperty("id")]
        public string Id;
        [JsonProperty("name")]
        public string Name;
    }
}