using System;
using PubNubChatAPI.Entities;
using PubnubChatApi.Utilities;

namespace PubnubChatApi.Entities.Data
{
    public struct UnreadMessageWrapper
    {
        public string ChannelId;
        public Membership Membership;
        public int Count;
    }
}