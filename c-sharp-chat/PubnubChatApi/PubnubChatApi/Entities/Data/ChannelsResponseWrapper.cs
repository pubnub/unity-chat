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
    }
}