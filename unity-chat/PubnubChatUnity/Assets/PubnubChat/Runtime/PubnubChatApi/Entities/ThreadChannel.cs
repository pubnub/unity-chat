using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;

namespace PubnubChatApi
{
    public class ThreadChannel : Channel
    {
        public string ParentChannelId { get; }
        public string ParentMessageTimeToken { get; }

        private bool initialised;

        internal ThreadChannel(Chat chat, string channelId, string parentChannelId, string parentMessageTimeToken,
            ChatChannelData data) : base(chat, channelId, data)
        {
            ParentChannelId = parentChannelId;
            ParentMessageTimeToken = parentMessageTimeToken;
            data.CustomData["parentChannelId"] = ParentChannelId;
            data.CustomData["parentMessageTimetoken"] = ParentMessageTimeToken;
        }

        private async Task<ChatOperationResult> InitThreadChannel()
        {
            var result = new ChatOperationResult("ThreadChannel.InitThreadChannel()", chat);
            var channelUpdate = await UpdateChannelData(chat, Id, channelData).ConfigureAwait(false);
            if (result.RegisterOperation(channelUpdate))
            {
                return result;
            }
            result.RegisterOperation(await chat.PubnubInstance.AddMessageAction()
                .Action(new PNMessageAction() { Type = "threadRootId", Value = Id }).Channel(ParentChannelId)
                .MessageTimetoken(long.Parse(ParentMessageTimeToken)).ExecuteAsync().ConfigureAwait(false));
            return result;
        }

        public override async Task<ChatOperationResult> SendText(string message, SendTextParams sendTextParams)
        {
            var result = new ChatOperationResult("ThreadChannel.SendText()", chat);
            if (!initialised)
            {
                if (result.RegisterOperation(await InitThreadChannel().ConfigureAwait(false)))
                {
                    return result;
                }

                initialised = true;
            }

            return await base.SendText(message, sendTextParams).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the message history for this thread channel.
        /// <para>
        /// Retrieves the list of messages from this thread within the specified time range and
        /// returns them as ThreadMessage objects that contain additional context about the parent channel.
        /// </para>
        /// </summary>
        /// <param name="startTimeToken">The start time token for the history range.</param>
        /// <param name="endTimeToken">The end time token for the history range.</param>
        /// <param name="count">The maximum number of messages to retrieve.</param>
        /// <returns>A ChatOperationResult containing the list of ThreadMessage objects from this thread.</returns>
        /// <example>
        /// <code>
        /// var threadChannel = // ...;
        /// var result = await threadChannel.GetThreadHistory("start_token", "end_token", 50);
        /// if (!result.Error) {
        ///     foreach (var threadMessage in result.Result) {
        ///         Console.WriteLine($"Thread message: {threadMessage.MessageText}");
        ///         Console.WriteLine($"Parent channel: {threadMessage.ParentChannelId}");
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="ThreadMessage"/>
        /// <seealso cref="GetMessageHistory"/>
        public async Task<ChatOperationResult<List<ThreadMessage>>> GetThreadHistory(string startTimeToken,
            string endTimeToken, int count)
        {
            var result = new ChatOperationResult<List<ThreadMessage>>("ThreadChannel.GetThreadHistory()", chat)
            {
                Result = new List<ThreadMessage>()
            };
            var getHistory = await GetMessageHistory(startTimeToken, endTimeToken, count).ConfigureAwait(false);
            if (result.RegisterOperation(getHistory))
            {
                return result;
            }

            foreach (var message in getHistory.Result)
            {
                result.Result.Add(new ThreadMessage(chat, message.TimeToken, message.OriginalMessageText,
                    message.ChannelId, ParentChannelId, message.UserId, PubnubChatMessageType.Text, message.Meta,
                    message.MessageActions, message.Files));
            }

            return result;
        }

        public override async Task<ChatOperationResult> EmitUserMention(string userId, string timeToken, string text)
        {
            var jsonDict = new Dictionary<string, string>()
            {
                {"text",text},
                {"messageTimetoken",timeToken},
                {"channel",Id},
                {"parentChannel", ParentChannelId}
            };
            return await chat.EmitEvent(PubnubChatEventType.Mention, userId,
                chat.PubnubInstance.JsonPluggableLibrary.SerializeToJsonString(jsonDict)).ConfigureAwait(false);
        }

        /// <summary>
        /// Pins a thread message to the parent channel.
        /// <para>
        /// Takes a message from this thread and pins it to the parent channel where the thread originated.
        /// This allows important thread messages to be highlighted in the main channel.
        /// </para>
        /// </summary>
        /// <param name="message">The thread message to pin to the parent channel.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var threadChannel = // ...;
        /// var threadMessage = // ... get a thread message
        /// var result = await threadChannel.PinMessageToParentChannel(threadMessage);
        /// if (!result.Error) {
        ///     // Thread message has been pinned to the parent channel
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="UnPinMessageFromParentChannel"/>
        /// <seealso cref="Chat.PinMessageToChannel"/>
        /// <seealso cref="ThreadMessage"/>
        public async Task<ChatOperationResult> PinMessageToParentChannel(ThreadMessage message)
        {
            return await chat.PinMessageToChannel(ParentChannelId, message).ConfigureAwait(false);
        }

        /// <summary>
        /// Unpins the currently pinned message from the parent channel.
        /// <para>
        /// Removes the pinned message from the parent channel where this thread originated.
        /// This undoes a previous pin operation performed by PinMessageToParentChannel.
        /// </para>
        /// </summary>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var threadChannel = // ...;
        /// var result = await threadChannel.UnPinMessageFromParentChannel();
        /// if (!result.Error) {
        ///     // Message has been unpinned from the parent channel
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="PinMessageToParentChannel"/>
        /// <seealso cref="Chat.UnpinMessageFromChannel"/>
        public async Task<ChatOperationResult> UnPinMessageFromParentChannel()
        {
            return await chat.UnpinMessageFromChannel(ParentChannelId).ConfigureAwait(false);
        }
    }
}