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

        internal UnreadMessageWrapper(Chat chat, InternalUnreadMessageWrapper internalWrapper)
        {
            Count = internalWrapper.Count;
            Channel = PointerParsers.ParseJsonChannelPointers(chat, new []{internalWrapper.Channel})[0];
            Membership = PointerParsers.ParseJsonMembershipPointers(chat, new []{internalWrapper.Membership})[0];
        }
    }

    internal struct InternalUnreadMessageWrapper
    {
        public IntPtr Channel;
        public IntPtr Membership;
        public int Count;
    }
}