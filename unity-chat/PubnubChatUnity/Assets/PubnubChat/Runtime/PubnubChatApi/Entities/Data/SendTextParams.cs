using System.Collections.Generic;

namespace PubnubChatApi
{
    public class SendTextParams
    {
        public bool StoreInHistory = true;
        public bool SendByPost = false;
        public Dictionary<string, object> Meta = new();
        public Dictionary<int, MentionedUser> MentionedUsers = new();
        public Message QuotedMessage = null;
        public List<ChatInputFile> Files = new();
        public Dictionary<string, string> CustomPushData = new();
    }
}