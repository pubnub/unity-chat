using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi
{
    public struct ChannelsResponseWrapper
    {
        public List<Channel> Channels;
        public PNPageObject Page;
        public int Total;
    }
}