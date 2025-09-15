using System;
using System.Collections.Generic;
using PubnubApi;
using PubNubChatAPI.Entities;
using PubnubChatApi.Utilities;

namespace PubnubChatApi.Entities.Data
{
    public struct UsersResponseWrapper
    {
        public List<User> Users;
        public PNPageObject Page;
        public int Total;
    }
}