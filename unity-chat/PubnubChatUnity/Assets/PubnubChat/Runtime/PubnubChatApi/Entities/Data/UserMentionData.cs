namespace PubnubChatApi
{
    public class UserMentionData
    {
        public string ChannelId;
        public string ParentChannelId;
        public string UserId;
        public ChatEvent Event;
        public Message Message;
    }
}