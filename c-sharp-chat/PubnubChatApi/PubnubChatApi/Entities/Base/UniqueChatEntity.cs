using System;

namespace PubNubChatAPI.Entities
{
    public abstract class UniqueChatEntity : ChatEntity
    {
        public string Id { get; protected set; }

        internal UniqueChatEntity(IntPtr pointer, string uniqueId) : base(pointer)
        {
            Id = uniqueId;
        }
    }
}