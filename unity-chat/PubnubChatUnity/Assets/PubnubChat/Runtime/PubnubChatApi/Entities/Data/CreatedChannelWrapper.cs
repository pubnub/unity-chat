using System.Collections.Generic;

namespace PubnubChatApi
{
    public class CreatedChannelWrapper
    {
        public Channel CreatedChannel;
        public Membership HostMembership;
        public List<Membership> InviteesMemberships;
    }
}