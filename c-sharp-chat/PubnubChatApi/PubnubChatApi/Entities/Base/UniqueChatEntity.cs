using System;

namespace PubNubChatAPI.Entities
{
    public abstract class UniqueChatEntity : ChatEntity
    {
        public string Id { get; protected set; }

        internal UniqueChatEntity(Chat chat, string uniqueId) : base(chat)
        {
            Id = uniqueId;
        }
    }
}