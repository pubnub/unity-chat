using PubnubApi;

namespace PubnubChatApi
{
    public class PubnubChatConfig
    {
        [System.Serializable]
        public class PushNotificationsConfig
        {
            public bool SendPushes;
            public string DeviceToken;
            public PNPushType DeviceGateway = PNPushType.FCM;
            public string APNSTopic;
            public PushEnvironment APNSEnvironment = PushEnvironment.Development;
        }
        
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
        public bool SyncMutedUsers { get; }
        public PushNotificationsConfig PushNotifications { get; }
        
        public PubnubChatConfig(int typingTimeout = 5000, int typingTimeoutDifference = 1000, int rateLimitFactor = 2,
            RateLimitPerChannel rateLimitPerChannel = null, bool storeUserActivityTimestamp = false,
            int storeUserActivityInterval = 60000, bool syncMutedUsers = false, PushNotificationsConfig pushNotifications = null)
        {
            RateLimitsPerChannel = rateLimitPerChannel ?? new RateLimitPerChannel();
            RateLimitFactor = rateLimitFactor;
            StoreUserActivityTimestamp = storeUserActivityTimestamp;
            StoreUserActivityInterval = storeUserActivityInterval;
            TypingTimeout = typingTimeout;
            TypingTimeoutDifference = typingTimeoutDifference;
            SyncMutedUsers = syncMutedUsers;
            PushNotifications = pushNotifications ?? new PushNotificationsConfig();
        }
    }
}