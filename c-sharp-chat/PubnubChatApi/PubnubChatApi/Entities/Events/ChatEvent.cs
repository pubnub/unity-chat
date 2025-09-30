namespace PubnubChatApi
{
    public struct ChatEvent
    {
        public string TimeToken;
        public PubnubChatEventType Type;
        public string ChannelId;
        public string UserId;
        public string Payload;
    }
}