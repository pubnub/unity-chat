using System;
using System.Collections.Generic;
using PubNubChatAPI.Entities;
using PubnubChatApi.Utilities;

namespace PubnubChatApi.Entities.Data
{
    public struct UsersResponseWrapper
    {
        public List<User> Users;
        public Page Page;
        public int Total;

        internal UsersResponseWrapper(Chat chat, InternalUsersResponseWrapper internalWrapper)
        {
            Page = internalWrapper.Page;
            Total = internalWrapper.Total;
            Users = PointerParsers.ParseJsonUserPointers(chat, internalWrapper.Users);
        }
    }

    internal struct InternalUsersResponseWrapper
    {
        public IntPtr[] Users;
        public Page Page;
        public int Total;
    }
}