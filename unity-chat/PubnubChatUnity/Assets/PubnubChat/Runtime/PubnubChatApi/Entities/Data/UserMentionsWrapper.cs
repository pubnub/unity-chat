using System.Collections.Generic;
using PubNubChatAPI.Entities;

namespace PubnubChatApi.Entities.Data
{
    public class UserMentionsWrapper
    {
        public List<UserMentionData> Mentions = new();
        public bool IsMore;
    }
}