using System;

namespace PubNubChatAPI.Entities
{
    public abstract class UniqueChatEntity : ChatEntity
    {
        public string Id { get; protected set; }

        internal UniqueChatEntity(string uniqueId)
        {
            Id = uniqueId;
        }
    }
}