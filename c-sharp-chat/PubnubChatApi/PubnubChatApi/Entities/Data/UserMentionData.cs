using System;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Events;

namespace PubnubChatApi.Entities.Data
{
    public class UserMentionData
    {
        public string ChannelId;
        public string UserId;
        public ChatEvent Event;
        public Message Message;

        public string ParentChannelId;
        public string ThreadChannelId;

        internal UserMentionData(Chat chat, InternalUserMentionData internalWrapper)
        {
            ChannelId = internalWrapper.ChannelId;
            UserId = internalWrapper.UserId;
            Event = internalWrapper.Event;
            chat.TryGetMessage(internalWrapper.Message, out Message);
            ParentChannelId = internalWrapper.ParentChannelId;
            ThreadChannelId = internalWrapper.ThreadChannelId;
        }
    }
    
    internal class InternalUserMentionData
    {
        public string ChannelId;
        public string UserId;
        public ChatEvent Event;
        public IntPtr Message;

        public string ParentChannelId;
        public string ThreadChannelId;
    }
}