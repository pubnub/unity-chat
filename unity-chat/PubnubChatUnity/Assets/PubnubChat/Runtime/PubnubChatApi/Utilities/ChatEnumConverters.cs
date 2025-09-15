using System;
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
        
        internal static PubnubChatEventType StringToEventType(string eventString)
        {
            switch (eventString)
            {
                case "typing":
                    return PubnubChatEventType.Typing;
                case "report":
                    return PubnubChatEventType.Report;
                case "receipt":
                    return PubnubChatEventType.Receipt;
                case "mention":
                    return PubnubChatEventType.Mention;
                case "invite":
                    return PubnubChatEventType.Invite;
                case "custom":
                    return PubnubChatEventType.Custom;
                case "moderation":
                    return PubnubChatEventType.Moderation;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        internal static PubnubMessageActionType StringToActionType(string actionString)
        {
            switch (actionString)
            {
                case "reaction":
                    return PubnubMessageActionType.Reaction;
                case "receipt":
                    return PubnubMessageActionType.Receipt;
                case "custom":
                    return PubnubMessageActionType.Custom;
                case "edited":
                    return PubnubMessageActionType.Edited;
                case "deleted":
                    return PubnubMessageActionType.Deleted;
                case "threadRootId":
                    return PubnubMessageActionType.ThreadRootId;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}