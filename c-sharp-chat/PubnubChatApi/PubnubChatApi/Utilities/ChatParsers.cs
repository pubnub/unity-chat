using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PubnubApi;
using PubNubChatAPI.Entities;
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
                
                //TODO: C# FIX, USER METADATA SHOULD BE A DICTIONARY<string, object>
                var meta = new Dictionary<string, object>();
                if (messageResult.UserMetadata != null)
                {
                    //TODO: REMOVE AS SOON AS C# FIX
                    var metaJObject = (JObject)messageResult.UserMetadata;
                    foreach (var kvp in metaJObject)
                    {
                        meta.Add(kvp.Key, kvp.Value.ToString());
                    }
                }
                
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
    }
}