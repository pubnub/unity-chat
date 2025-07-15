using System.Collections.Generic;
using PubNubChatAPI.Entities;

namespace PubnubChatApi.Entities.Data
{
    public class CreatedChannelWrapper
    {
        public Channel CreatedChannel;
        public Membership HostMembership;
        public List<Membership> InviteesMemberships;
    }
}