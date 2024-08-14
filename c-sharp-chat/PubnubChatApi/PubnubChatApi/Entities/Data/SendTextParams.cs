using System.Collections.Generic;
using PubNubChatAPI.Entities;

namespace PubnubChatApi.Entities.Data
{
    public class SendTextParams
    {
        public bool StoreInHistory = true;
        public bool SendByPost = false;
        public string Meta = string.Empty;
        public Dictionary<int, User> MentionedUsers = new();
        public Dictionary<int, Channel> ReferencedChannels = new();
        public List<TextLink> TextLinks = new();
        public Message QuotedMessage = null;
    }
}