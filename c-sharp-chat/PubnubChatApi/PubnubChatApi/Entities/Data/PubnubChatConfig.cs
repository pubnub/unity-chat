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
        
        /// <para>Enable automatic syncing of the Chat.MutedUsersManager data with App Context,
        /// using the current userId as the key.</para>
        /// Specifically, the data is saved in the custom object of the following User in App Context:
        /// <code>PN_PRIV.{userId}.mute.1</code>
        /// where userId is the current com.pubnub.api.v2.PNConfiguration.userId
        /// <para>If using Access Manager, the access token must be configured with the appropriate rights to subscribe to that
        /// channel, and get, update, and delete the App Context User with that id.</para>
        /// <para>Due to App Context size limits, the number of muted users is limited to around 200 and will result in sync errors
        /// when the limit is exceeded. The list will not sync until its size is reduced.</para>
        public bool SyncMutedUsers { get; }

        public PubnubChatConfig(string publishKey, string subscribeKey, string userId, string authKey = "",
            int typingTimeout = 5000, int typingTimeoutDifference = 1000, int rateLimitFactor = 2,
            RateLimitPerChannel rateLimitPerChannel = null, bool storeUserActivityTimestamp = false,
            int storeUserActivityInterval = 60000, bool syncMutedUsers = false)
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
            SyncMutedUsers = syncMutedUsers;
        }
    }
}