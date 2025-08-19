using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PubnubApi;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Enums;

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

                //TODO: later more types I guess?
                var type = PubnubChatMessageType.Text; //messageDict["type"].ToString();
                var text = messageDict["text"].ToString();
                
                var meta = messageResult.UserMetadata ?? new Dictionary<string, object>();
                
                message = new Message(chat, messageResult.Timetoken.ToString(), text, messageResult.Channel, messageResult.Publisher, type, meta);
                return true;
            }
            catch (Exception e)
            {
                chat.Logger.Debug($"Failed to parse PNMessageResult with payload: {messageResult.Message} into chat Message entity. Exception was: {e.Message}");
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
    }
}