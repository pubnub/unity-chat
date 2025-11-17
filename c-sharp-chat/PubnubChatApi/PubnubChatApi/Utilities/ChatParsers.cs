using System;
using System.Collections.Generic;
using System.Linq;
using PubnubApi;

namespace PubnubChatApi
{
    internal static class ChatParsers
    {
        internal static bool TryParseMessageResult(Chat chat, PNMessageResult<object> messageResult, out Message message)
        {
            try
            {
                var messageDict =
                    chat.PubnubInstance.JsonPluggableLibrary.DeserializeToDictionaryOfObject(messageResult.Message
                        .ToString());

                if (!messageDict.TryGetValue("type", out var typeValue) || typeValue.ToString() != "text")
                {
                    message = null;
                    return false;
                }

                //TODO: later more types I guess?
                var type = PubnubChatMessageType.Text;
                var text = messageDict["text"].ToString();
                var meta = messageResult.UserMetadata ?? new Dictionary<string, object>();
                
                message = new Message(chat, messageResult.Timetoken.ToString(), text, messageResult.Channel, messageResult.Publisher, type, meta, new List<MessageAction>());
                return true;
            }
            catch (Exception e)
            {
                chat.Logger.Debug($"Failed to parse PNMessageResult with payload: {messageResult.Message} into chat Message entity. Exception was: {e.Message}");
                message = null;
                return false;
            }
        }
        
        internal static bool TryParseMessageFromHistory(Chat chat, string channelId, PNHistoryItemResult historyItem, out Message message)
        {
            try
            {
                var messageDict =
                    chat.PubnubInstance.JsonPluggableLibrary.DeserializeToDictionaryOfObject(historyItem.Entry.ToString());

                if (!messageDict.TryGetValue("type", out var typeValue) || typeValue.ToString() != "text")
                {
                    message = null;
                    return false;
                }

                //TODO: later more types I guess?
                var type = PubnubChatMessageType.Text;
                var text = messageDict["text"].ToString();
                
                var actions = new List<MessageAction>();
                if (historyItem.ActionItems != null)
                {
                    foreach (var kvp in historyItem.ActionItems)
                    {
                        var actionType = ChatEnumConverters.StringToActionType(kvp.Key);
                        foreach (var actionEntry in kvp.Value)
                        {
                            actions.Add(new MessageAction()
                            {
                                TimeToken = actionEntry.ActionTimetoken.ToString(),
                                UserId = actionEntry.Uuid,
                                Type = actionType,
                                Value = actionEntry.Action.Value
                            });
                        }
                    }
                }
                message = new Message(chat, historyItem.Timetoken.ToString(), text, channelId, historyItem.Uuid, type, historyItem.Meta, actions);
                return true;
            }
            catch (Exception e)
            {
                chat.Logger.Debug($"Failed to parse PNHistoryItemResult with payload: {historyItem.Entry} into chat Message entity. Exception was: {e.Message}");
                message = null;
                return false;
            }
        }

        internal static bool TryParseMembershipUpdate(Chat chat, Membership membership, PNObjectEventResult objectEvent, out ChatMembershipData updatedData, out ChatEntityChangeType changeType)
        {
            try
            {
                var channel = objectEvent.MembershipMetadata.Channel;
                var user = objectEvent.MembershipMetadata.Uuid;
                var type = objectEvent.Type;
                changeType = objectEvent.Event switch
                {
                    "set" => ChatEntityChangeType.Updated,
                    "delete" => ChatEntityChangeType.Deleted,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (type == "membership" && channel == membership.ChannelId && user == membership.UserId)
                {
                    updatedData = new ChatMembershipData()
                    {
                        Status = objectEvent.MembershipMetadata.Status,
                        CustomData = objectEvent.MembershipMetadata.Custom,
                        Type = objectEvent.MembershipMetadata.Type
                    };
                    return true;
                }
                else
                {
                    updatedData = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                chat.Logger.Debug($"Failed to parse PNObjectEventResult of type: {objectEvent.Event} into Membership update. Exception was: {e.Message}");
                updatedData = null;
                changeType = default;
                return false;
            }
        }
        
        internal static bool TryParseUserUpdate(Chat chat, User user, PNObjectEventResult objectEvent, out ChatUserData updatedData, out ChatEntityChangeType changeType)
        {
            try
            {
                var uuid = objectEvent.UuidMetadata.Uuid;
                var type = objectEvent.Type;
                changeType = objectEvent.Event switch
                {
                    "set" => ChatEntityChangeType.Updated,
                    "delete" => ChatEntityChangeType.Deleted,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (type == "uuid" && uuid == user.Id)
                {
                    updatedData = objectEvent.UuidMetadata;
                    return true;
                }
                else
                {
                    updatedData = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                chat.Logger.Debug($"Failed to parse PNObjectEventResult of type: {objectEvent.Event} into User update. Exception was: {e.Message}");
                updatedData = null;
                changeType = default;
                return false;
            }
        }
        
        internal static bool TryParseChannelUpdate(Chat chat, Channel channel, PNObjectEventResult objectEvent, out ChatChannelData updatedData, out ChatEntityChangeType changeType)
        {
            try
            {
                var channelId = objectEvent.Channel;
                var type = objectEvent.Type;
                changeType = objectEvent.Event switch
                {
                    "set" => ChatEntityChangeType.Updated,
                    "delete" => ChatEntityChangeType.Deleted,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (type == "channel" && channelId == channel.Id)
                {
                    updatedData = objectEvent.ChannelMetadata;
                    return true;
                }
                else
                {
                    updatedData = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                chat.Logger.Debug($"Failed to parse PNObjectEventResult of type: {objectEvent.Event} into Channel update. Exception was: {e.Message}");
                updatedData = null;
                changeType = default;
                return false;
            }
        }
        
        internal static bool TryParseMessageUpdate(Chat chat, Message message, PNMessageActionEventResult actionEvent)
        {
            try
            {
                if (actionEvent.MessageTimetoken.ToString() == message.TimeToken && actionEvent.Uuid == message.UserId && actionEvent.Channel == message.ChannelId)
                {
                    if (actionEvent.Event != "removed")
                    {
                        //already has it
                        if (message.MessageActions.Any(x => x.TimeToken == actionEvent.ActionTimetoken.ToString()))
                        {
                            return true;
                        }
                        message.MessageActions.Add(new MessageAction()
                        {
                            TimeToken = actionEvent.ActionTimetoken.ToString(),
                            Type = ChatEnumConverters.StringToActionType(actionEvent.Action.Type),
                            Value = actionEvent.Action.Value,
                            UserId = actionEvent.Uuid
                        });
                    }
                    else
                    {
                        var dict = message.MessageActions.ToDictionary(x => x.TimeToken, y => y);
                        dict.Remove(actionEvent.ActionTimetoken.ToString());
                        message.MessageActions = dict.Values.ToList();
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                chat.Logger.Debug($"Failed to parse PNMessageActionEventResult into Message update. Exception was: {e.Message}");
                return false;
            }
        }

        internal static bool TryParseEvent(Chat chat, PNMessageResult<object> messageResult, PubnubChatEventType eventType, out ChatEvent chatEvent)
        {
            try
            {
                var jsonDict =
                    chat.PubnubInstance.JsonPluggableLibrary.DeserializeToDictionaryOfObject(messageResult.Message
                        .ToString());
                if (!jsonDict.TryGetValue("type", out var typeString))
                {
                    chatEvent = default;
                    return false;
                }
                var receivedEventType = ChatEnumConverters.StringToEventType(typeString.ToString());
                if (receivedEventType != eventType)
                {
                    chatEvent = default;
                    return false;
                }
                if (chat.MutedUsersManager.MutedUsers.Contains(messageResult.Publisher))
                {
                    chatEvent = default;
                    return false;
                }
                chatEvent = new ChatEvent()
                {
                    TimeToken = messageResult.Timetoken.ToString(),
                    Type = eventType,
                    ChannelId = messageResult.Channel,
                    UserId = messageResult.Publisher,
                    Payload = messageResult.Message.ToString()
                };
                return true;
            }
            catch (Exception e)
            {
                chat.Logger.Debug($"Failed to parse PNMessageResult into ChatEvent of type \"{eventType}\". Exception was: {e.Message}");
                chatEvent = default;
                return false;
            }
        }
        
        internal static bool TryParseEventFromHistory(Chat chat, string channelId, PNHistoryItemResult historyItem, out ChatEvent chatEvent)
        {
            try
            {
                var jsonDict =
                    chat.PubnubInstance.JsonPluggableLibrary.DeserializeToDictionaryOfObject(historyItem.Entry.ToString());
                if (!jsonDict.TryGetValue("type", out var typeString))
                {
                    chatEvent = default;
                    return false;
                }
                var receivedEventType = ChatEnumConverters.StringToEventType(typeString.ToString());
                chatEvent = new ChatEvent()
                {
                    TimeToken = historyItem.Timetoken.ToString(),
                    Type = receivedEventType,
                    ChannelId = channelId,
                    UserId = historyItem.Uuid,
                    Payload = historyItem.Entry.ToString()
                };
                return true;
            }
            catch (Exception e)
            {
                chat.Logger.Debug($"Failed to parse PNHistoryItemResult into ChatEvent. Exception was: {e.Message}");
                chatEvent = default;
                return false;
            }
        }
    }
}