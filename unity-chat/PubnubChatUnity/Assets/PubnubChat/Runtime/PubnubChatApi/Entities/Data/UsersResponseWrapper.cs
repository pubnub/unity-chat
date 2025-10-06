using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi
{
    public struct UsersResponseWrapper
    {
        public List<User> Users;
        public PNPageObject Page;
        public int Total;
    }
}