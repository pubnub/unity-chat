using System.Collections.Generic;
using PubnubChatApi.Enums;

namespace PubnubChatApi.Entities.Data
{
    public class PubnubChatConfig
    {
        public string PublishKey { get; }
        public string SubscribeKey { get; }
        public string UserId { get; }
        public string AuthKey { get; }
        public int TypingTimeout { get; }
        public int TypingTimeoutDifference { get; }
        public int RateLimitFactor { get; }
        public Dictionary<PubnubChannelType, int> RateLimitPerChannel { get; }
        public bool StoreUserActivityTimestamp { get; }
        public int StoreUserActivityInterval { get; }

        public PubnubChatConfig(string publishKey, string subscribeKey, string userId, string authKey = "",
            int typingTimeout = 5000, int typingTimeoutDifference = 1000, int rateLimitFactor = 2,
            Dictionary<PubnubChannelType, int> rateLimitPerChannel = null, bool storeUserActivityTimestamp = false,
            int storeUserActivityInterval = 60000)
        {
            RateLimitPerChannel = (rateLimitPerChannel == null) 
                ? new()
                {
                    { PubnubChannelType.Direct, 2 },
                    { PubnubChannelType.Group, 2 },
                    { PubnubChannelType.Public, 2 },
                } 
                : rateLimitPerChannel;
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