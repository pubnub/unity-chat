using System;
using PubNubChatAPI.Entities;
using PubnubChatApi.Utilities;

namespace PubnubChatApi.Entities.Data
{
    public struct UnreadMessageWrapper
    {
        public Channel Channel;
        public Membership Membership;
        public int Count;
    }
}