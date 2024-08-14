using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern void pn_message_delete(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_message_edit_text(IntPtr message, string text);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_text(IntPtr message, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_message_delete_message(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_delete_message_hard(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_deleted(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_get_timetoken(IntPtr message, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_get_data_type(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern void pn_message_get_data_text(IntPtr message, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_message_get_data_channel_id(IntPtr message, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_message_get_data_user_id(IntPtr message, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_message_get_data_meta(IntPtr message, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_get_data_message_actions(IntPtr message, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_pin(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_get_reactions(IntPtr message, StringBuilder reactions_json);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_message_toggle_reaction(IntPtr message, string reaction);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_has_user_reaction(IntPtr message, string reaction);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_report(IntPtr message, string reason);
        
        [DllImport("pubnub-chat")]
        private static extern int pn_message_has_thread(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_message_update_with_base_message(IntPtr message, IntPtr base_message);
        
        [DllImport("pubnub-chat")]
        private static extern int pn_message_mentioned_users(IntPtr message, IntPtr chat, StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern int pn_message_referenced_channels(IntPtr message, IntPtr chat, StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_message_quoted_message(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern int pn_message_text_links(IntPtr message, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_message_restore(IntPtr message);
        
        #endregion

        /// <summary>
        /// The text content of the message.
        /// <para>
        /// This is the main content of the message. It can be any text that the user wants to send.
        /// </para>
        /// </summary>
        public string MessageText
        {
            get
            {
                var buffer = new StringBuilder(32768);
                CUtilities.CheckCFunctionResult(pn_message_text(pointer, buffer));
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The time token of the message.
        /// <para>
        /// The time token is a unique identifier for the message.
        /// It is used to identify the message in the chat.
        /// </para>
        /// </summary>
        public string TimeToken
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_message_get_timetoken(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The channel ID of the channel that the message belongs to.
        /// <para>
        /// This is the ID of the channel that the message was sent to.
        /// </para>
        /// </summary>
        public string ChannelId
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_message_get_data_channel_id(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The user ID of the user that sent the message.
        /// <para>
        /// This is the unique ID of the user that sent the message.
        /// Do not confuse this with the username of the user.
        /// </para>
        /// </summary>
        public string UserId
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_message_get_data_user_id(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The metadata of the message.
        /// <para>
        /// The metadata is additional data that can be attached to the message.
        /// It can be used to store additional information about the message.
        /// </para>
        /// </summary>
        public string Meta
        {
            get
            {
                var buffer = new StringBuilder(4096);
                pn_message_get_data_meta(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// Whether the message has been deleted.
        /// <para>
        /// This property indicates whether the message has been deleted.
        /// If the message has been deleted, this property will be true.
        /// It means that all the deletions are soft deletions.
        /// </para>
        /// </summary>
        public bool IsDeleted
        {
            get
            {
                var result = pn_message_deleted(pointer);
                CUtilities.CheckCFunctionResult(result);
                return result == 1;
            }
        }

        public List<User> MentionedUsers
        {
            get
            {
                var buffer = new StringBuilder(1024);
                CUtilities.CheckCFunctionResult(pn_message_mentioned_users(pointer, chat.Pointer, buffer));
                var usersJson = buffer.ToString();
                if (!CUtilities.IsValidJson(usersJson))
                {
                    return new List<User>();
                }
                var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, IntPtr[]>>(usersJson);
                if (jsonDict == null || !jsonDict.TryGetValue("value", out var pointers) || pointers == null)
                {
                    return new List<User>();
                }
                return PointerParsers.ParseJsonUserPointers(chat, pointers);
            }
        }
        
        public List<Channel> ReferencedChannels
        {
            get
            {
                var buffer = new StringBuilder(1024);
                CUtilities.CheckCFunctionResult(pn_message_referenced_channels(pointer, chat.Pointer, buffer));
                var channelsJson = buffer.ToString();
                if (!CUtilities.IsValidJson(channelsJson))
                {
                    return new List<Channel>();
                }
                var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, IntPtr[]>>(channelsJson);
                if (jsonDict == null || !jsonDict.TryGetValue("value", out var pointers) || pointers == null)
                {
                    return new List<Channel>();
                }
                return PointerParsers.ParseJsonChannelPointers(chat, pointers);
            }
        }
        
        public List<TextLink> TextLinks
        {
            get
            {
                var buffer = new StringBuilder(2048);
                CUtilities.CheckCFunctionResult(pn_message_text_links(pointer, buffer));
                var jsonString = buffer.ToString();
                if (!CUtilities.IsValidJson(jsonString))
                {
                    return new List<TextLink>();
                }
                var textLinks = JsonConvert.DeserializeObject<Dictionary<string, List<TextLink>>>(jsonString);
                if (textLinks == null || !textLinks.TryGetValue("value", out var links) || links == null)
                {
                    return new List<TextLink>();
                }
                return links;
            }
        }

        private List<MessageAction> DeserializeMessageActions(string json)
        {
            var reactions = new List<MessageAction>();
            if (CUtilities.IsValidJson(json))
            {
                reactions = JsonConvert.DeserializeObject<List<MessageAction>>(json);
                reactions ??= new List<MessageAction>();
            }
            return reactions;
        }
        
        public List<MessageAction> MessageActions
        {
            get
            {
                var buffer = new StringBuilder(4096);
                CUtilities.CheckCFunctionResult(pn_message_get_data_message_actions(pointer, buffer));
                return DeserializeMessageActions(buffer.ToString());
            }
        }
        
        public List<MessageAction> Reactions
        {
            get
            {
                var buffer = new StringBuilder(4096);
                CUtilities.CheckCFunctionResult(pn_message_get_reactions(pointer, buffer));
                return DeserializeMessageActions(buffer.ToString());
            }
        }

        /// <summary>
        /// The data type of the message.
        /// <para>
        /// This is the type of the message data.
        /// It can be used to determine the type of the message.
        /// </para>
        /// </summary>
        /// <seealso cref="pubnub_chat_message_type"/>
        public PubnubChatMessageType Type => (PubnubChatMessageType)pn_message_get_data_type(pointer);

        protected Chat chat;

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
        
        public override void StartListeningForUpdates()
        {
            //TODO: hacky way to subscribe to this channel
            chat.ListenForEvents(ChannelId, PubnubChatEventType.Custom);
        }

        internal Message(Chat chat, IntPtr messagePointer, string timeToken) : base(messagePointer, timeToken)
        {
            this.chat = chat;
        }

        internal static string GetMessageIdFromPtr(IntPtr messagePointer)
        {
            var buffer = new StringBuilder(512);
            pn_message_get_timetoken(messagePointer, buffer);
            return buffer.ToString();
        }

        internal static string GetChannelIdFromMessagePtr(IntPtr messagePointer)
        {
            var buffer = new StringBuilder(512);
            pn_message_get_data_channel_id(messagePointer, buffer);
            return buffer.ToString();
        }
        
        internal override void UpdateWithPartialPtr(IntPtr partialPointer)
        {
            var newFullPointer = pn_message_update_with_base_message(partialPointer, pointer);
            CUtilities.CheckCFunctionResult(newFullPointer);
            UpdatePointer(newFullPointer);
        }

        internal virtual void BroadcastMessageUpdate()
        {
            Debug.WriteLine($"MESSAGE UPDATE - {MessageText}");
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
        public void EditMessageText(string newText)
        {
            var newPointer = pn_message_edit_text(pointer, newText);
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }

        public bool TryGetQuotedMessage(out Message quotedMessage)
        {
            var quotedMessagePointer = pn_message_quoted_message(pointer);
            if (quotedMessagePointer == IntPtr.Zero)
            {
                Debug.WriteLine(CUtilities.GetErrorMessage());
                quotedMessage = null;
                return false;
            }
            return chat.TryGetMessage(quotedMessagePointer, out quotedMessage);
        }

        public bool HasThread()
        {
            var result = pn_message_has_thread(pointer);
            CUtilities.CheckCFunctionResult(result);
            return result == 1;
        }

        public ThreadChannel CreateThread()
        {
            return chat.CreateThreadChannel(this);
        }

        public bool TryGetThread(out ThreadChannel threadChannel)
        {
            return chat.TryGetThreadChannel(this, out threadChannel);
        }

        public void RemoveThread()
        {
            chat.RemoveThreadChannel(this);
        }

        public void Pin()
        {
            CUtilities.CheckCFunctionResult(pn_message_pin(pointer));
        }

        public void Report(string reason)
        {
            CUtilities.CheckCFunctionResult(pn_message_report(pointer, reason));
        }

        public void Forward(string channelId)
        {
            if (chat.TryGetChannel(channelId, out var channel))
            {
                chat.ForwardMessage(this, channel);
            }
        }

        public bool HasUserReaction(string reactionValue)
        {
            var result = pn_message_has_user_reaction(pointer, reactionValue);
            CUtilities.CheckCFunctionResult(result);
            return result == 1;
        }

        public void ToggleReaction(string reactionValue)
        {
            var newPointer = pn_message_toggle_reaction(pointer, reactionValue);
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }

        public void Restore()
        {
            var newPointer = pn_message_restore(pointer);
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
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
        public void Delete(bool soft)
        {
            if (soft)
            {
                var newPointer = pn_message_delete_message(pointer);
                CUtilities.CheckCFunctionResult(newPointer);
                UpdatePointer(newPointer);
            }
            else
            {
                CUtilities.CheckCFunctionResult(pn_message_delete_message_hard(pointer));
            }
        }

        protected override void DisposePointer()
        {
            pn_message_delete(pointer);
        }
    }
}