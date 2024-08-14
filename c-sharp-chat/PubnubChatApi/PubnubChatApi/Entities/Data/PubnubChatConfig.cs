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

        public PubnubChatConfig(string publishKey, string subscribeKey, string userId, string authKey = "", int typingTimeout = 5000, int typingTimeoutDifference = 1000)
        {
            PublishKey = publishKey;
            SubscribeKey = subscribeKey;
            UserId = userId;
            AuthKey = authKey;
            TypingTimeout = typingTimeout;
            TypingTimeoutDifference = typingTimeoutDifference;
        }
    }
}