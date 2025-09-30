using System;

namespace PubnubChatApi
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