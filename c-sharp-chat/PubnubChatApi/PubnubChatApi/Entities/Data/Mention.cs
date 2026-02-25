namespace PubnubChatApi
{
    public struct Mention
    {
        public string MessageTimetoken;
        public string ChannelId;
        public string? ParentChannelId;
        public string MentionedByUserId;
    }
}