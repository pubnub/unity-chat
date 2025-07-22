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
    }
}