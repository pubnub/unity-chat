namespace PubnubChatApi
{
    public struct MessageAction
    {
        public PubnubMessageActionType Type;
        public string Value;
        public string TimeToken;
        public string UserId;
    }
}