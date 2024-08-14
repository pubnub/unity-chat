using System.Collections.Generic;

namespace PubnubChatApi.Entities.Data
{
    public class ChannelsRestrictionsWrapper
    {
        public List<ChannelRestriction> Restrictions = new ();
        public Page Page = new ();
        public int Total;
        public string Status = string.Empty;
    }
}