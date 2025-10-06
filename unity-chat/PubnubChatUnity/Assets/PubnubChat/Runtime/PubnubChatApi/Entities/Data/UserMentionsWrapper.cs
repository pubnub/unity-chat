using System.Collections.Generic;

namespace PubnubChatApi
{
    public class UserMentionsWrapper
    {
        public List<UserMentionData> Mentions = new();
        public bool IsMore;
    }
}