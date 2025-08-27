using System;
using System.Collections.Generic;
using System.Linq;
using PubnubApi;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Entities.Events;
using PubnubChatApi.Enums;
using Channel = PubNubChatAPI.Entities.Channel;

namespace PubnubChatApi.Utilities
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
                
                //TODO: C# FIX, Meta shouldn't be an object but a deserialized type
                var meta = chat.PubnubInstance.JsonPluggableLibrary.ConvertToDictionaryObject(historyItem.Meta) ?? new Dictionary<string, object>();
                
                //TODO: C# FIX, Actions shouldn't be an object but a deserialized type
                var actions = new List<MessageAction>();
                if (historyItem.Actions is Dictionary<string, object> actionsDict)
                {
                    foreach (var kvp in actionsDict)
                    {
                        var actionType = kvp.Key;
                        var entries = kvp.Value as Dictionary<string, object>;
                        foreach (var entryPair in entries)
                        {
                            var actionValue = entryPair.Key;
                            var actionEntries = entryPair.Value as List<object>;
                            foreach (var entry in actionEntries)
                            {
                                var entryDict = entry as Dictionary<string, object>;
                                actions.Add(new MessageAction()
                                {
                                    TimeToken = entryDict["actionTimetoken"].ToString(),
                                    UserId = entryDict["uuid"].ToString(),
                                    Type = ChatEnumConverters.StringToActionType(actionType),
                                    Value = actionValue
                                });
                            }
                        }
                    }
                }
                
                message = new Message(chat, historyItem.Timetoken.ToString(), text, channelId, historyItem.Uuid, type, meta, actions);
                return true;
            }
            catch (Exception e)
            {
                chat.Logger.Debug($"Failed to parse PNHistoryItemResult with payload: {historyItem.Entry} into chat Message entity. Exception was: {e.Message}");
                message = null;
                return false;
            }
        }

        internal static bool TryParseMembershipUpdate(Chat chat, Membership membership, PNObjectEventResult objectEvent, out ChatMembershipData updatedData)
        {
            try
            {
                var channel = objectEvent.ChannelMetadata.Channel;
                var user = objectEvent.UuidMetadata.Uuid;
                var type = objectEvent.Type;
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
                return false;
            }
        }
        
        internal static bool TryParseUserUpdate(Chat chat, User user, PNObjectEventResult objectEvent, out ChatUserData updatedData)
        {
            try
            {
                var uuid = objectEvent.UuidMetadata.Uuid;
                var type = objectEvent.Type;
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
                return false;
            }
        }
        
        internal static bool TryParseChannelUpdate(Chat chat, Channel channel, PNObjectEventResult objectEvent, out ChatChannelData updatedData)
        {
            try
            {
                var channelId = objectEvent.Channel;
                var type = objectEvent.Type;
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