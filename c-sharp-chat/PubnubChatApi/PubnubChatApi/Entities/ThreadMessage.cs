using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Enums;

namespace PubNubChatAPI.Entities
{
    public class ThreadMessage : Message
    {
        public event Action<ThreadMessage> OnThreadMessageUpdated;
        
        public string ParentChannelId
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        
        internal ThreadMessage(Chat chat, string timeToken, string originalMessageText, string channelId, string userId, PubnubChatMessageType type, Dictionary<string, object> meta) : base(chat, timeToken, originalMessageText, channelId, userId, type, meta)
        {
        }

        /// <summary>
        /// Edits the text of the message.
        /// <para>
        /// This method edits the text of the message.
        /// It changes the text of the message to the new text provided.
        /// </para>
        /// </summary>
        /// <param name="newText">The new text of the message.</param>
        /// <example>
        /// <code>
        /// var message = // ...;
        /// message.EditMessageText("New text");
        /// </code>
        /// </example>
        /// <seealso cref="OnMessageUpdated"/>
        public override async Task EditMessageText(string newText)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetQuotedMessage(out Message quotedMessage)
        {
            throw new NotImplementedException();
        }
        
        public override async Task<ChatOperationResult> Report(string reason)
        {
            throw new NotImplementedException();
        }

        public override async Task<ChatOperationResult> Forward(string channelId)
        {
            throw new NotImplementedException();
        }

        public override bool HasUserReaction(string reactionValue)
        {
            throw new NotImplementedException();
        }

        public override async Task ToggleReaction(string reactionValue)
        {
            throw new NotImplementedException();
        }

        public override async Task Restore()
        {
            throw new NotImplementedException();
        }
        
        public override async Task Delete(bool soft)
        {
            throw new NotImplementedException();
        }

        public async Task PinMessageToParentChannel()
        {
            throw new NotImplementedException();
        }

        public async Task UnPinMessageFromParentChannel()
        {
            throw new NotImplementedException();
        }
        
    }
}