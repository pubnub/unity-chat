using System;
using System.Collections.Generic;
using PubNubChatAPI.Entities;
using PubnubChatApi.Utilities;

namespace PubnubChatApi.Entities.Data
{
    public struct MarkMessagesAsReadWrapper
    {
        public Page Page;
        public int Total;
        public int Status;
        public List<Membership> Memberships;

        internal MarkMessagesAsReadWrapper(Chat chat, InternalMarkMessagesAsReadWrapper internalWrapper)
        {
            Page = internalWrapper.Page;
            Total = internalWrapper.Total;
            Status = internalWrapper.Status;
            Memberships = PointerParsers.ParseJsonMembershipPointers(chat, internalWrapper.Memberships);
        }
    }

    internal struct InternalMarkMessagesAsReadWrapper
    {
        public Page Page;
        public int Total;
        public int Status;
        public IntPtr[] Memberships;
    }
}