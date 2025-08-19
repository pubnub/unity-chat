using System;
using System.Collections.Generic;
using PubnubApi;
using PubnubChatApi.Utilities;
using Channel = PubNubChatAPI.Entities.Channel;

namespace PubnubChatApi.Entities.Data
{
    public struct ChannelsResponseWrapper
    {
        public List<Channel> Channels;
        public PNPageObject Page;
        public int Total;
    }
}