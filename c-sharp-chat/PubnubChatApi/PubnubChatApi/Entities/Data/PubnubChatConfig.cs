namespace PubnubChatApi.Entities.Data
{
    public class PubnubChatConfig
    {
        [System.Serializable]
        public class RateLimitPerChannel
        {
            public int DirectConversation;
            public int GroupConversation;
            public int PublicConversation;
            public int UnknownConversation;
        }
        
        public string PublishKey { get; }
        public string SubscribeKey { get; }
        public string UserId { get; }
        public string AuthKey { get; }
        public int TypingTimeout { get; }
        public int TypingTimeoutDifference { get; }
        public int RateLimitFactor { get; }
        public RateLimitPerChannel RateLimitsPerChannel { get; }
        public bool StoreUserActivityTimestamp { get; }
        public int StoreUserActivityInterval { get; }

        public PubnubChatConfig(string publishKey, string subscribeKey, string userId, string authKey = "",
            int typingTimeout = 5000, int typingTimeoutDifference = 1000, int rateLimitFactor = 2,
            RateLimitPerChannel rateLimitPerChannel = null, bool storeUserActivityTimestamp = false,
            int storeUserActivityInterval = 60000)
        {
            RateLimitsPerChannel = rateLimitPerChannel;
            RateLimitFactor = rateLimitFactor;
            StoreUserActivityTimestamp = storeUserActivityTimestamp;
            StoreUserActivityInterval = storeUserActivityInterval;
            PublishKey = publishKey;
            SubscribeKey = subscribeKey;
            UserId = userId;
            AuthKey = authKey;
            TypingTimeout = typingTimeout;
            TypingTimeoutDifference = typingTimeoutDifference;
        }
    }
}