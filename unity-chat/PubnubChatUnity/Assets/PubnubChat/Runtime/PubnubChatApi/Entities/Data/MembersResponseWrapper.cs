using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi
{
    public class MembersResponseWrapper
    {
        public List<Membership> Memberships = new ();
        public PNPageObject Page = new ();
        public int Total;
        public string Status;
    }
}