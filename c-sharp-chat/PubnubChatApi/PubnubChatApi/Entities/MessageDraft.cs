using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public enum MentionType
    {
        User,
        Channel,
        Url
    }

    public class MentionTarget
    {
        [JsonProperty("type")]
        public MentionType Type { get; set; }
        [JsonProperty("target")]
        public string Target { get; set; }
    }
    
    public class SuggestedMention
    {
        public int Offset { get; set; }
        public string ReplaceFrom { get; set; }
        public string ReplaceTo { get; set; }
        public MentionTarget Target { get; set; }
    };

    public class MessageElement
    {
        public string Text { get; set; }
        public MentionTarget? MentionTarget { get; set; } = null;
    }
    
    /// <summary>
    /// Enum describing the source for getting user suggestions for mentions.
    /// </summary>
    public enum UserSuggestionSource
    {
        /// <summary>
        /// Search for users globally.
        /// </summary>
        GLOBAL,

        /// <summary>
        /// Search only for users that are members of this channel.
        /// </summary>
        CHANNEL
    }
    
    public class MessageDraft
    {
        private class DraftCallbackDataHelper
        {
            public List<MessageElement> MessageElements;
            public List<SuggestedMention> SuggestedMentions;
        }
        
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern void pn_message_draft_delete(IntPtr message_draft);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_draft_insert_text(IntPtr message_draft, int position,
            string text_to_insert);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_draft_remove_text(IntPtr message_draft, int position, int length);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_draft_insert_suggested_mention(IntPtr message_draft, int offset,
            string replace_from, string replace_to, string target_json, string text);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_draft_add_mention(IntPtr message_draft, int start, int length,
            string target);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_draft_remove_mention(IntPtr message_draft, int start);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_draft_update(IntPtr message_draft, string text);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_draft_send(IntPtr message_draft,
            bool store_in_history,
            bool send_by_post,
            string meta,
            int mentioned_users_length,
            int[] mentioned_users_indexes,
            IntPtr[] mentioned_users,
            IntPtr quoted_message);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_draft_consume_callback_data(IntPtr message_draft, StringBuilder data);

        [DllImport("pubnub-chat")]
        private static extern void pn_message_draft_set_search_for_suggestions(IntPtr message_draft,
            bool search_for_suggestions);

        #endregion

        private IntPtr pointer;
        
        public event Action<List<MessageElement>, List<SuggestedMention>> OnDraftUpdated; 
        
        //TODO: will see if these stay non-accessible
        /*
        /// <summary>
        /// The Channel where this MessageDraft will be published.
        /// </summary>
        public Channel Channel { get; }
        /// <summary>
        /// The scope for searching for suggested users - either [UserSuggestionSource.GLOBAL] or [UserSuggestionSource.CHANNEL].
        /// </summary>
        private UserSuggestionSource UserSuggestionSourceSetting { get; }
        /// <summary>
        /// Whether modifying the message text triggers the typing indicator on [channel].
        /// </summary>
        public bool IsTypingIndicatorTriggered { get; }
        /// <summary>
        /// The limit on the number of users returned when searching for users to mention.
        /// </summary>
        public int UserLimit { get; }
        /// <summary>
        /// The limit on the number of channels returned when searching for channels to reference.
        /// </summary>
        public int ChannelLimit { get; }
        /// <summary>
        /// Can be used to set a [Message] to quote when sending this [MessageDraft].
        /// </summary>
        public Message QuotedMessage { get; }*/

        internal MessageDraft(IntPtr pointer)
        {
            this.pointer = pointer;
        }

        private void BroadcastDraftUpdate()
        {
            var buffer = new StringBuilder(4096);
            CUtilities.CheckCFunctionResult(pn_message_draft_consume_callback_data(pointer, buffer));
            var callbackDataJson = buffer.ToString();
            var callbackData = JsonConvert.DeserializeObject<DraftCallbackDataHelper>(callbackDataJson);
            if (callbackData == null)
            {
                return;
            }
            OnDraftUpdated?.Invoke(callbackData.MessageElements, callbackData.SuggestedMentions);
        }
        
        /// <summary>
        /// Insert some text into the MessageDraft text at the given offset.
        /// </summary>
        /// <param name="offset">The position from the start of the message draft where insertion will occur</param>
        /// <param name="text">Text the text to insert at the given offset</param>
        public void InsertText(int offset, string text)
        {
            CUtilities.CheckCFunctionResult(pn_message_draft_insert_text(pointer, offset, text));
            BroadcastDraftUpdate();
        }

        /// <summary>
        /// Remove a number of characters from the MessageDraft text at the given offset.
        /// </summary>
        /// <param name="offset">The position from the start of the message draft where removal will occur</param>
        /// <param name="length">Length the number of characters to remove, starting at the given offset</param>
        public void RemoveText(int offset, int length)
        {
            CUtilities.CheckCFunctionResult(pn_message_draft_remove_text(pointer, offset, length));
            BroadcastDraftUpdate();
        }

        /// <summary>
        /// Insert mention into the MessageDraft according to SuggestedMention.Offset, SuggestedMention.ReplaceFrom and
        /// SuggestedMention.target.
        /// </summary>
        /// <param name="mention">A SuggestedMention that can be obtained from MessageDraftStateListener</param>
        /// <param name="text">The text to replace SuggestedMention.ReplaceFrom with. SuggestedMention.ReplaceTo can be used for example.</param>
        public void InsertSuggestedMention(SuggestedMention mention, string text)
        {
            var jsonMentionTarget = JsonConvert.SerializeObject(mention.Target);
            CUtilities.CheckCFunctionResult(pn_message_draft_insert_suggested_mention(pointer, mention.Offset,
                mention.ReplaceFrom, mention.ReplaceTo, jsonMentionTarget, text));
            BroadcastDraftUpdate();
        }

        /// <summary>
        /// Add a mention to a user, channel or link specified by target at the given offset.
        /// </summary>
        /// <param name="offset">The start of the mention</param>
        /// <param name="length">The number of characters (length) of the mention</param>
        /// <param name="target">The target of the mention</param>
        public void AddMention(int offset, int length, MentionTarget target)
        {
            var jsonMentionTarget = JsonConvert.SerializeObject(target);
            CUtilities.CheckCFunctionResult(pn_message_draft_add_mention(pointer, offset, length, jsonMentionTarget));
            BroadcastDraftUpdate();
        }

        /// <summary>
        /// Remove a mention starting at the given offset, if any.
        /// </summary>
        /// <param name="offset">Offset the start of the mention to remove</param>
        public void RemoveMention(int offset)
        {
            CUtilities.CheckCFunctionResult(pn_message_draft_remove_mention(pointer, offset));
            BroadcastDraftUpdate();
        }

        /// <summary>
        /// Update the whole message draft text with a new value.
        /// Internally MessageDraft will try to calculate the most
        /// optimal set of insertions and removals that will convert the current text to the provided text, in order to
        /// preserve any mentions. This is a best effort operation, and if any mention text is found to be modified,
        /// the mention will be invalidated and removed.
        /// </summary>
        /// <param name="text"></param>
        public void Update(string text)
        {
            CUtilities.CheckCFunctionResult(pn_message_draft_update(pointer, text));
            BroadcastDraftUpdate();
        }

        /// <summary>
        /// Send the MessageDraft, along with its quotedMessage if any, on the channel.
        /// </summary>
        public async Task Send()
        {
            await Send(new SendTextParams());
        }

        /// <summary>
        /// Send the MessageDraft, along with its quotedMessage if any, on the channel.
        /// </summary>
        /// <param name="sendTextParams">Additional parameters for sending the message.</param>
        public async Task Send(SendTextParams sendTextParams)
        {
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_message_draft_send(
                pointer,
                sendTextParams.StoreInHistory,
                sendTextParams.SendByPost,
                sendTextParams.Meta,
                sendTextParams.MentionedUsers.Count,
                sendTextParams.MentionedUsers.Keys.ToArray(),
                sendTextParams.MentionedUsers.Values.Select(x => x.Pointer).ToArray(),
                sendTextParams.QuotedMessage == null ? IntPtr.Zero : sendTextParams.QuotedMessage.Pointer)));
        }

        public void SetSearchForSuggestions(bool searchForSuggestions)
        {
            pn_message_draft_set_search_for_suggestions(pointer, searchForSuggestions);
        }

        ~MessageDraft()
        {
            pn_message_draft_delete(pointer);
        }
    }
}