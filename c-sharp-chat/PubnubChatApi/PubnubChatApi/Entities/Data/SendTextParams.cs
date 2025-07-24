using System.Collections.Generic;
using PubNubChatAPI.Entities;

namespace PubnubChatApi.Entities.Data
{
    public class SendTextParams
    {
        public bool StoreInHistory = true;
        public bool SendByPost = false;
        public string Meta = string.Empty;
        public Dictionary<int, MentionedUser> MentionedUsers = new();
        public Message QuotedMessage = null;
    }
}