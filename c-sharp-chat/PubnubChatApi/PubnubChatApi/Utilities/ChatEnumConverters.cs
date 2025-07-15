using PubnubChatApi.Enums;

namespace PubnubChatApi.Utilities
{
    internal static class ChatEnumConverters
    {
        internal static string ChatEventTypeToString(PubnubChatEventType eventType)
        {
            switch(eventType)
            {
                case PubnubChatEventType.Typing:
                    return "typing";
                case PubnubChatEventType.Report:
                    return "report";
                case PubnubChatEventType.Receipt:
                    return "receipt";
                case PubnubChatEventType.Mention:
                    return "mention";
                case PubnubChatEventType.Invite:
                    return "invite";
                case PubnubChatEventType.Custom:
                    return "custom";
                case PubnubChatEventType.Moderation:
                    return "moderation";
                default:
                    return "incorrect_chat_event_type";
                    break;
            }
        }
    }
}