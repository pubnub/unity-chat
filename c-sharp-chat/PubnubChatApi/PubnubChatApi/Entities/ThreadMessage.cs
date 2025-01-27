using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public class ThreadMessage : Message
    {
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern void pn_thread_message_dispose(IntPtr thread_message);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_edit_text(
            IntPtr message,
            string text);

        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_text(IntPtr message, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_delete_message(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_delete_message_hard(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_deleted(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern void pn_thread_message_get_timetoken(IntPtr message, StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_get_data_type(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern void pn_thread_message_get_data_text(IntPtr message, StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern void pn_thread_message_get_data_channel_id(IntPtr message, StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern void pn_thread_message_get_data_user_id(IntPtr message, StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern void pn_thread_message_get_data_meta(IntPtr message, StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_get_data_message_actions(IntPtr message, StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_pin(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_get_reactions(IntPtr message, StringBuilder reactions_json);
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_toggle_reaction(IntPtr message, string reaction);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_has_user_reaction(IntPtr message, string reaction);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_report(IntPtr message, string reason);
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_create_thread(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_has_thread(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_get_thread(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_remove_thread(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_update_with_base_message(IntPtr message, IntPtr base_message);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_mentioned_users(IntPtr message, IntPtr chat, StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_referenced_channels(IntPtr message, IntPtr chat,
            StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_quoted_message(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_text_links(IntPtr message, StringBuilder result);
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_restore(IntPtr message);
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_consume_and_upgrade(
            IntPtr message,
            string parent_channel_id);
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_unpin_from_parent_channel(IntPtr thread_message);
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_pin_to_parent_channel(IntPtr thread_message);
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_parent_channel_id(IntPtr thread_message, StringBuilder result);

        #endregion

        public event Action<ThreadMessage> OnThreadMessageUpdated;

        //TODO: some code repetition with Message in these property overrides
        /// <summary>
        /// The text content of the message.
        /// <para>
        /// This is the main content of the message. It can be any text that the user wants to send.
        /// </para>
        /// </summary>
        public override string MessageText
        {
            get
            {
                var buffer = new StringBuilder(32768);
                CUtilities.CheckCFunctionResult(pn_thread_message_text(pointer, buffer));
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
        public override string TimeToken
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_thread_message_get_timetoken(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The channel ID of the channel that the message belongs to.
        /// <para>
        /// This is the ID of the channel that the message was sent to.
        /// </para>
        /// </summary>
        public override string ChannelId
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_thread_message_get_data_channel_id(pointer, buffer);
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
        public override string UserId
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_thread_message_get_data_user_id(pointer, buffer);
                return buffer.ToString();
            }
        }
        
        public override PubnubChatMessageType Type => (PubnubChatMessageType)pn_thread_message_get_data_type(pointer);

        /// <summary>
        /// The metadata of the message.
        /// <para>
        /// The metadata is additional data that can be attached to the message.
        /// It can be used to store additional information about the message.
        /// </para>
        /// </summary>
        public override string Meta
        {
            get
            {
                var buffer = new StringBuilder(4096);
                pn_thread_message_get_data_meta(pointer, buffer);
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
        public override bool IsDeleted
        {
            get
            {
                var result = pn_thread_message_deleted(pointer);
                CUtilities.CheckCFunctionResult(result);
                return result == 1;
            }
        }

        public override List<User> MentionedUsers
        {
            get
            {
                var buffer = new StringBuilder(1024);
                CUtilities.CheckCFunctionResult(pn_thread_message_mentioned_users(pointer, chat.Pointer, buffer));
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
        
        public override List<Channel> ReferencedChannels
        {
            get
            {
                var buffer = new StringBuilder(1024);
                CUtilities.CheckCFunctionResult(pn_thread_message_referenced_channels(pointer, chat.Pointer, buffer));
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
        
        public override List<TextLink> TextLinks
        {
            get
            {
                var buffer = new StringBuilder(2048);
                CUtilities.CheckCFunctionResult(pn_thread_message_text_links(pointer, buffer));
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
        
        public override List<MessageAction> MessageActions
        {
            get
            {
                var buffer = new StringBuilder(4096);
                CUtilities.CheckCFunctionResult(pn_thread_message_get_data_message_actions(pointer, buffer));
                return DeserializeMessageActions(buffer.ToString());
            }
        }
        
        public override List<MessageAction> Reactions
        {
            get
            {
                var buffer = new StringBuilder(4096);
                CUtilities.CheckCFunctionResult(pn_thread_message_get_reactions(pointer, buffer));
                return DeserializeMessageActions(buffer.ToString());
            }
        }
        
        public string ParentChannelId
        {
            get
            {
                var buffer = new StringBuilder(128);
                CUtilities.CheckCFunctionResult(pn_thread_message_parent_channel_id(pointer, buffer));
                return buffer.ToString();
            }
        }

        internal ThreadMessage(Chat chat, IntPtr messagePointer, string timeToken) : base(chat, messagePointer,
            timeToken)
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
            var newPointer = await Task.Run(() => pn_thread_message_edit_text(pointer, newText));
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }

        public override bool TryGetQuotedMessage(out Message quotedMessage)
        {
            var quotedMessagePointer = pn_thread_message_quoted_message(pointer);
            if (quotedMessagePointer == IntPtr.Zero)
            {
                Debug.WriteLine(CUtilities.GetErrorMessage());
                quotedMessage = null;
                return false;
            }
            return chat.TryGetMessage(quotedMessagePointer, out quotedMessage);
        }
        
        public override async Task Report(string reason)
        {
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_thread_message_report(pointer, reason)));
        }

        public override async Task Forward(string channelId)
        {
            if (chat.TryGetChannel(channelId, out var channel))
            {
                await chat.ForwardMessage(this, channel);
            }
        }

        public override bool HasUserReaction(string reactionValue)
        {
            var result = pn_thread_message_has_user_reaction(pointer, reactionValue);
            CUtilities.CheckCFunctionResult(result);
            return result == 1;
        }

        public override async Task ToggleReaction(string reactionValue)
        {
            var newPointer = await Task.Run(() => pn_thread_message_toggle_reaction(pointer, reactionValue));
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }

        public override async Task Restore()
        {
            var newPointer = await Task.Run(() => pn_thread_message_restore(pointer));
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }
        
        public override async Task Delete(bool soft)
        {
            await Task.Run(() =>
            {
                if (soft)
                {
                    var newPointer = pn_thread_message_delete_message(pointer);
                    CUtilities.CheckCFunctionResult(newPointer);
                    UpdatePointer(newPointer);
                }
                else
                {
                    CUtilities.CheckCFunctionResult(pn_thread_message_delete_message_hard(pointer));
                }
            });
        }

        internal override void BroadcastMessageUpdate()
        {
            base.BroadcastMessageUpdate();
            OnThreadMessageUpdated?.Invoke(this);
        }

        internal static string GetThreadMessageIdFromPtr(IntPtr threadMessagePointer)
        {
            var buffer = new StringBuilder(128);
            pn_thread_message_get_timetoken(threadMessagePointer, buffer);
            return buffer.ToString();
        }

        internal override void UpdateWithPartialPtr(IntPtr partialPointer)
        {
            var newFullPointer = pn_thread_message_update_with_base_message(partialPointer, pointer);
            CUtilities.CheckCFunctionResult(newFullPointer);
            UpdatePointer(newFullPointer);
        }

        public async Task PinMessageToParentChannel()
        {
            var newChannelPointer = await Task.Run(() => pn_thread_message_pin_to_parent_channel(pointer));
            CUtilities.CheckCFunctionResult(newChannelPointer);
            chat.UpdateChannelPointer(newChannelPointer);
        }

        public async Task UnPinMessageFromParentChannel()
        {
            var newChannelPointer = await Task.Run(() => pn_thread_message_unpin_from_parent_channel(pointer));
            CUtilities.CheckCFunctionResult(newChannelPointer);
            chat.UpdateChannelPointer(newChannelPointer);
        }

        protected override void DisposePointer()
        {
            pn_thread_message_dispose(pointer);
        }
    }
}