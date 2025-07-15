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

        internal MembersResponseWrapper()
        {
        }

        internal MembersResponseWrapper(Chat chat, InternalMembersResponseWrapper internalWrapper)
        {
            Page = internalWrapper.Page;
            Total = internalWrapper.Total;
            Status = internalWrapper.Status;
            Memberships = PointerParsers.ParseJsonMembershipPointers(chat, internalWrapper.Memberships);
        }
    }
    
    internal struct InternalMembersResponseWrapper
    {
        public IntPtr[] Memberships;
        public Page Page;
        public int Total;
        public string Status;
    }
}