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
    }
}