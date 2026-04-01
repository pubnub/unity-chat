namespace PubnubChatApi
{
    public struct Mention
    {
        public string Text;
        public string MessageTimetoken;
        public string ChannelId;
        public string? ParentChannelId;
        public string MentionedByUserId;
    }
}