using System.Collections.Generic;

namespace PubnubChatApi.Entities.Data
{
    public class UsersRestrictionsWrapper
    {
        public List<UserRestriction> Restrictions = new ();
        public Page Page = new ();
        public int Total;
        public string Status = string.Empty;
    }
}