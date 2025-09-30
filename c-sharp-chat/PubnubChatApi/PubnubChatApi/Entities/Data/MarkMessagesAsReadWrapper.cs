using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi
{
    public struct MarkMessagesAsReadWrapper
    {
        public PNPageObject Page;
        public int Total;
        public string Status;
        public List<Membership> Memberships;
    }
}