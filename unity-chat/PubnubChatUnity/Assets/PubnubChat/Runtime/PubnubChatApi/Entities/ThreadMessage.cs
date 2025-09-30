using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;

namespace PubnubChatApi
{
    public class ThreadMessage : Message
    {
        public event Action<ThreadMessage> OnThreadMessageUpdated;

        public string ParentChannelId { get; }

        internal ThreadMessage(Chat chat, string timeToken, string originalMessageText, string channelId,
            string parentChannelId, string userId, PubnubChatMessageType type, Dictionary<string, object> meta,
            List<MessageAction> messageActions) : base(chat, timeToken, originalMessageText, channelId, userId, type,
            meta, messageActions)
        {
            ParentChannelId = parentChannelId;
        }

        protected override SubscribeCallback CreateUpdateListener()
        {
            return chat.ListenerFactory.ProduceListener(
                messageActionCallback: delegate(Pubnub pn, PNMessageActionEventResult e)
                {
                    if (ChatParsers.TryParseMessageUpdate(chat, this, e))
                    {
                        OnThreadMessageUpdated?.Invoke(this);
                    }
                });
        }

        /// <summary>
        /// Pins this thread message to the parent channel.
        /// <para>
        /// Takes this message from the thread and pins it to the parent channel where the thread originated.
        /// This allows important thread messages to be highlighted in the main channel for all users to see.
        /// </para>
        /// </summary>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var threadMessage = // ... get a thread message
        /// var result = await threadMessage.PinMessageToParentChannel();
        /// if (!result.Error) {
        ///     // This thread message has been pinned to the parent channel
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="UnPinMessageFromParentChannel"/>
        /// <seealso cref="Pin"/>
        /// <seealso cref="Chat.PinMessageToChannel"/>
        /// <seealso cref="ThreadChannel.PinMessageToParentChannel"/>
        public async Task<ChatOperationResult> PinMessageToParentChannel()
        {
            return await chat.PinMessageToChannel(ParentChannelId, this).ConfigureAwait(false);
        }

        /// <summary>
        /// Unpins the currently pinned message from the parent channel.
        /// <para>
        /// Removes the pinned message from the parent channel where this thread originated.
        /// This is typically used when this thread message was previously pinned to the parent channel
        /// and now needs to be unpinned.
        /// </para>
        /// </summary>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var threadMessage = // ... get a thread message
        /// var result = await threadMessage.UnPinMessageFromParentChannel();
        /// if (!result.Error) {
        ///     // Message has been unpinned from the parent channel
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="PinMessageToParentChannel"/>
        /// <seealso cref="Chat.UnpinMessageFromChannel"/>
        /// <seealso cref="ThreadChannel.UnPinMessageFromParentChannel"/>
        public async Task<ChatOperationResult> UnPinMessageFromParentChannel()
        {
            return await chat.UnpinMessageFromChannel(ParentChannelId).ConfigureAwait(false);
        }
    }
}