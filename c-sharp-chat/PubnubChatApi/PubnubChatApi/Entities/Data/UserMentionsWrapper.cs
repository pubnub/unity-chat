using System.Collections.Generic;
using PubNubChatAPI.Entities;

namespace PubnubChatApi.Entities.Data
{
    public class UserMentionsWrapper
    {
        public List<UserMentionData> Mentions = new();
        public bool IsMore;

        internal UserMentionsWrapper(Chat chat, InternalUserMentionsWrapper internalWrapper)
        {
            IsMore = internalWrapper.IsMore;
            foreach (var internalMention in internalWrapper.UserMentionData)
            {
                Mentions.Add(new UserMentionData(chat, internalMention));
            }
        }
    }
    
    internal class InternalUserMentionsWrapper
    {
        public List<InternalUserMentionData> UserMentionData = new();
        public bool IsMore;
    }
}