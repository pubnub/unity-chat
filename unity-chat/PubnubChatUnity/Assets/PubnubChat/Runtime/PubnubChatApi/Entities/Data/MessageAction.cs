using PubnubChatApi.Enums;

namespace PubnubChatApi.Entities.Data
{
    public struct MessageAction
    {
        public PubnubMessageActionType Type;
        public string Value;
        public string TimeToken;
        public string UserId;
    }
}