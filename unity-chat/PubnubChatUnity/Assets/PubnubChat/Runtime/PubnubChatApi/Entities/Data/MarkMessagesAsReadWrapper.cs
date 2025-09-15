using System;
using System.Collections.Generic;
using PubnubApi;
using PubNubChatAPI.Entities;
using PubnubChatApi.Utilities;

namespace PubnubChatApi.Entities.Data
{
    public struct MarkMessagesAsReadWrapper
    {
        public PNPageObject Page;
        public int Total;
        public string Status;
        public List<Membership> Memberships;
    }
}