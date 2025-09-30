using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi
{
    public class ChannelsRestrictionsWrapper
    {
        public List<ChannelRestriction> Restrictions = new ();
        public PNPageObject Page = new ();
        public int Total;
    }
}