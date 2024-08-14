using System;
using System.Collections.Generic;
using PubNubChatAPI.Entities;
using PubnubChatApi.Utilities;

namespace PubnubChatApi.Entities.Data
{
    public struct ChannelsResponseWrapper
    {
        public List<Channel> Channels;
        public Page Page;
        public int Total;

        internal ChannelsResponseWrapper(Chat chat, InternalChannelsResponseWrapper internalWrapper)
        {
            Page = internalWrapper.Page;
            Total = internalWrapper.Total;
            Channels = PointerParsers.ParseJsonChannelPointers(chat, internalWrapper.Channels);
        }
    }

    internal struct InternalChannelsResponseWrapper
    {
        public IntPtr[] Channels;
        public Page Page;
        public int Total;
    }
}