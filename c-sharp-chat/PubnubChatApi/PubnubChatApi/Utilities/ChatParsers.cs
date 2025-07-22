using System;
using System.Collections.Generic;
using PubnubApi;
using PubNubChatAPI.Entities;

namespace PubnubChatApi.Utilities
{
    internal static class ChatParsers
    {
        internal static bool TryParseMessageResult(Chat chat, PNMessageResult<object> messageResult, out Message message)
        {
            try
            {
                //TODO: don't know if UserMetadata is a JSON string or a Dict so we'll see if this breaks I guess?
                var meta = messageResult.UserMetadata as Dictionary<string, object>;
                message = new Message(chat, messageResult.Timetoken.ToString(), messageResult.Message.ToString(), messageResult.Channel, messageResult.Publisher, meta);
                return true;
            }
            catch (Exception e)
            {
                message = null;
                return false;
            }
        }
    }
}