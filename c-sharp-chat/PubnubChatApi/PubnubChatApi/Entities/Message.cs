using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    /// <summary>
    /// Represents a message in a chat channel.
    /// <para>
    /// Messages are sent by users to chat channels. They can contain text
    /// and other data, such as metadata or message actions.
    /// </para>
    /// </summary>
    /// <seealso cref="Chat"/>
    /// <seealso cref="Channel"/>
    public class Message : UniqueChatEntity
    {
        protected Chat chat;

        /// <summary>
        /// The text content of the message.
        /// <para>
        /// This is the main content of the message. It can be any text that the user wants to send.
        /// </para>
        /// </summary>
        public virtual string MessageText {
            get
            {
                var edits = MessageActions.Where(x => x.Type == PubnubMessageActionType.Edited).ToList();
                return edits.Any() ? edits[0].Value : OriginalMessageText;
            }
        }

        /// <summary>
        /// The original, un-edited text of the message.
        /// </summary>
        public virtual string OriginalMessageText { get; internal set; }

        /// <summary>
        /// The time token of the message.
        /// <para>
        /// The time token is a unique identifier for the message.
        /// It is used to identify the message in the chat.
        /// </para>
        /// </summary>
        public virtual string TimeToken { get; internal set; }

        /// <summary>
        /// The channel ID of the channel that the message belongs to.
        /// <para>
        /// This is the ID of the channel that the message was sent to.
        /// </para>
        /// </summary>
        public virtual string ChannelId { get; internal set; }

        /// <summary>
        /// The user ID of the user that sent the message.
        /// <para>
        /// This is the unique ID of the user that sent the message.
        /// Do not confuse this with the username of the user.
        /// </para>
        /// </summary>
        public virtual string UserId { get; internal set; }

        /// <summary>
        /// The metadata of the message.
        /// <para>
        /// The metadata is additional data that can be attached to the message.
        /// It can be used to store additional information about the message.
        /// </para>
        /// </summary>
        public Dictionary<string, object> Meta { get; internal set; } = new ();

        /// <summary>
        /// Whether the message has been deleted.
        /// <para>
        /// This property indicates whether the message has been deleted.
        /// If the message has been deleted, this property will be true.
        /// It means that all the deletions are soft deletions.
        /// </para>
        /// </summary>
        public virtual bool IsDeleted => MessageActions.Any(x => x.Type == PubnubMessageActionType.Deleted);
        
        public virtual List<MentionedUser> MentionedUsers {
            get
            {
                var mentioned = new List<MentionedUser>();
                if (Meta.TryGetValue("mentionedUsers", out var rawMentionedUsers))
                {
                    //TODO: might break
                    var deserialized = chat.PubnubInstance.JsonPluggableLibrary.DeserializeToObject<List<MentionedUser>>(rawMentionedUsers.ToString());
                    mentioned.AddRange(deserialized);
                }
                return mentioned;
            }
        }

        public virtual List<MessageAction> MessageActions { get; internal set; } = new();

        public virtual List<MessageAction> Reactions =>
            MessageActions.Where(x => x.Type == PubnubMessageActionType.Reaction).ToList();

        /// <summary>
        /// The data type of the message.
        /// <para>
        /// This is the type of the message data.
        /// It can be used to determine the type of the message.
        /// </para>
        /// </summary>
        /// <seealso cref="pubnub_chat_message_type"/>
        public virtual PubnubChatMessageType Type { get; internal set; }


        /// <summary>
        /// Event that is triggered when the message is updated.
        /// <para>
        /// This event is triggered when the message is updated by the server.
        /// Every time the message is updated, this event is triggered.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// var message = // ...;
        /// message.OnMessageUpdated += (message) =>
        /// {
        ///   Console.WriteLine("Message updated!");
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="EditMessageText"/>
        /// <seealso cref="Delete"/>
        public event Action<Message> OnMessageUpdated;

        internal Message(Chat chat, string timeToken,string originalMessageText, string channelId, string userId, PubnubChatMessageType type, Dictionary<string, object> meta) : base(timeToken)
        {
            this.chat = chat;
            TimeToken = timeToken;
            OriginalMessageText = originalMessageText;
            ChannelId = channelId;
            UserId = userId;
            Type = type;
            Meta = meta;
        }

        internal virtual void BroadcastMessageUpdate()
        {
            OnMessageUpdated?.Invoke(this);
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
        public virtual async Task EditMessageText(string newText)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryGetQuotedMessage(out Message quotedMessage)
        {
            throw new NotImplementedException();
        }

        public bool HasThread()
        {
            throw new NotImplementedException();
        }

        public async Task<ThreadChannel> CreateThread()
        {
            return await chat.CreateThreadChannel(this);
        }

        /// <summary>
        /// Tries to get the ThreadChannel started on this Message.
        /// </summary>
        /// <param name="threadChannel">The retrieved ThreadChannel object, null if one wasn't found.</param>
        /// <returns>True if a ThreadChannel object has been found, false otherwise.</returns>
        /// <seealso cref="GetThreadAsync"/>
        public bool TryGetThread(out ThreadChannel threadChannel)
        {
            return chat.TryGetThreadChannel(this, out threadChannel);
        }

        /// <summary>
        /// Asynchronously tries to get the ThreadChannel started on this Message.
        /// </summary>
        /// <returns>The retrieved ThreadChannel object, null if one wasn't found.</returns>
        public async Task<ThreadChannel?> GetThreadAsync()
        {
            return await chat.GetThreadChannelAsync(this);
        }

        public async Task RemoveThread()
        {
            await chat.RemoveThreadChannel(this);
        }

        public async Task Pin()
        {
            throw new NotImplementedException();
        }

        public virtual async Task Report(string reason)
        {
            throw new NotImplementedException();
        }

        public virtual async Task Forward(string channelId)
        {
            var channel = await chat.GetChannelAsync(channelId);
            if (channel != null)
            {
                await chat.ForwardMessage(this, channel);
            }
        }

        public virtual bool HasUserReaction(string reactionValue)
        {
            throw new NotImplementedException();
        }

        public virtual async Task ToggleReaction(string reactionValue)
        {
            throw new NotImplementedException();
        }

        public virtual async Task Restore()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes the message.
        /// <para>
        /// This method deletes the message.
        /// It marks the message as deleted.
        /// It means that the message will not be visible to other users, but the 
        /// message is treated as soft deleted.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// var message = // ...;
        /// message.DeleteMessage();
        /// </code>
        /// </example>
        /// <seealso cref="IsDeleted"/>
        /// <seealso cref="OnMessageUpdated"/>
        public virtual async Task Delete(bool soft)
        {
            throw new NotImplementedException();
        }

        public override Task Resync()
        {
            throw new NotImplementedException();
        }
    }
}