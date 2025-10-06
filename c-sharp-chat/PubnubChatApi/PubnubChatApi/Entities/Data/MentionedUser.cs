using Newtonsoft.Json;

namespace PubnubChatApi
{
    public struct MentionedUser
    {
        [JsonProperty("id")]
        public string Id;
        [JsonProperty("name")]
        public string Name;
    }
}