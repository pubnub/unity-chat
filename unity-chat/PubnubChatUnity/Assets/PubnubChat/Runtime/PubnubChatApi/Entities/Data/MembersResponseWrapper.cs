using System;
using System.Collections.Generic;
using PubnubApi;
using PubNubChatAPI.Entities;
using PubnubChatApi.Utilities;

namespace PubnubChatApi.Entities.Data
{
    public class MembersResponseWrapper
    {
        public List<Membership> Memberships = new ();
        public PNPageObject Page = new ();
        public int Total;
        public string Status;
    }
}