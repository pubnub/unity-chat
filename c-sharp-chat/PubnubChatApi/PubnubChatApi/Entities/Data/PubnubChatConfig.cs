using PubnubApi;

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
        
        public int TypingTimeout { get; }
        public int TypingTimeoutDifference { get; }
        public int RateLimitFactor { get; }
        public RateLimitPerChannel RateLimitsPerChannel { get; }
        public bool StoreUserActivityTimestamp { get; }
        public int StoreUserActivityInterval { get; }
        
        public PubnubChatConfig(int typingTimeout = 5000, int typingTimeoutDifference = 1000, int rateLimitFactor = 2,
            RateLimitPerChannel rateLimitPerChannel = null, bool storeUserActivityTimestamp = false,
            int storeUserActivityInterval = 60000)
        {
            RateLimitsPerChannel = rateLimitPerChannel ?? new RateLimitPerChannel();
            RateLimitFactor = rateLimitFactor;
            StoreUserActivityTimestamp = storeUserActivityTimestamp;
            StoreUserActivityInterval = storeUserActivityInterval;
            TypingTimeout = typingTimeout;
            TypingTimeoutDifference = typingTimeoutDifference;
        }
    }
}