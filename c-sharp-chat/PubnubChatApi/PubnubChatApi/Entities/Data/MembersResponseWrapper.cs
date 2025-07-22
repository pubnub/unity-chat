using System;
using System.Collections.Generic;
using PubNubChatAPI.Entities;
using PubnubChatApi.Utilities;

namespace PubnubChatApi.Entities.Data
{
    public class MembersResponseWrapper
    {
        public List<Membership> Memberships = new ();
        public Page Page = new ();
        public int Total;
        public string Status;
    }
}