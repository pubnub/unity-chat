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

        //TODO: REMOVE
        internal UsersResponseWrapper(Chat chat, InternalUsersResponseWrapper internalWrapper)
        {
            Page = new PNPageObject()
            {
                Next = internalWrapper.Page.Next,
                Prev = internalWrapper.Page.Previous
            };
            Total = internalWrapper.Total;
            Users = PointerParsers.ParseJsonUserPointers(chat, internalWrapper.Users);
        }
    }

    //TODO: REMOVE
    internal struct InternalUsersResponseWrapper
    {
        public IntPtr[] Users;
        public Page Page;
        public int Total;
    }
}