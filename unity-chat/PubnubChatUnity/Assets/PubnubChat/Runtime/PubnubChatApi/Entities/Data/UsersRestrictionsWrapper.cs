using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi
{
    public class UsersRestrictionsWrapper
    {
        public List<UserRestriction> Restrictions = new ();
        public PNPageObject Page = new ();
        public int Total;
    }
}