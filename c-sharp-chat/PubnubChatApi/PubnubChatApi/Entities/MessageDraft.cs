using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PubNubChatAPI.Entities
{
    //TODO: the generic/inheritance/interface abstraction architecture of all this
    public abstract class MentionTarget
    {
        public string StringId { get; set; }
    }

    public class UserMentionTarget : MentionTarget
    {
    }

    public class ChannelMentionTarget : MentionTarget
    {
    }

    public class UrlMentionTarget : MentionTarget
    {
    }

    //TODO: IEquatable?
    public class Mention
    {
        public int Start { get; }
        public int Length { get; }
        public MentionTarget Target { get; }
        public int EndExclusive => Start + Length;
        public Mention(int start, int length, MentionTarget target)
        {
            Start = start;
            Length = length;
            Target = target;
        }
    }

    public class MessageElement
    {
        public string Text { get; set; }
    }

    public class Link : MessageElement
    {
        public MentionTarget MentionTarget { get; set; }
    };

    //TODO: fix inline documentation format
    public class MessageDraft : ChatEntity
    {
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

        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern void pn_message_draft_delete(IntPtr message_draft);

        #endregion

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
        public Message QuotedMessage { get; }

        internal MessageDraft(Channel channel, UserSuggestionSource userSuggestionSource, IntPtr pointer,
            bool isTypingIndicatorTriggered, int userLimit, int channelLimit, Message quotedMessage) :
            base(pointer)
        {
            Channel = channel;
            UserSuggestionSourceSetting = userSuggestionSource;
            IsTypingIndicatorTriggered = isTypingIndicatorTriggered;
            UserLimit = userLimit;
            ChannelLimit = channelLimit;
            QuotedMessage = quotedMessage;
        }

        /**
         * Can be used to attach files to send with this [MessageDraft].
         */
        //val files: MutableList<InputFile>

        //TODO: events
        /**
         * Add a [MessageDraftStateListener] to listen for changes to the contents of this [MessageDraft], as well as
         * to retrieve the current mention suggestions for users and channels (e.g. when the message draft contains
         * "... @name ..." or "... #chann ...")
         *
         * @param callback the [MessageDraftStateListener] that will receive the most current message elements list and
         * suggestions list.
         */
        //fun addMessageElementsListener(callback: MessageDraftStateListener)

        /**
         * Remove the given [MessageDraftStateListener] from active listeners.
         */
        //fun removeMessageElementsListener(callback: MessageDraftStateListener)

        /// <summary>
        /// Insert some text into the [MessageDraft] text at the given offset.
        /// </summary>
        /// <param name="offset">The position from the start of the message draft where insertion will occur</param>
        /// <param name="text">Text the text to insert at the given offset</param>
        public void InsertText(int offset, string text)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove a number of characters from the [MessageDraft] text at the given offset.
        /// </summary>
        /// <param name="offset">The position from the start of the message draft where removal will occur</param>
        /// <param name="length">Length the number of characters to remove, starting at the given offset</param>
        public void RemoveText(int offset, int length)
        {
            throw new NotImplementedException();
        }

        //TODO: will have to see about the SuggestedMention here
        /**
         * Insert mention into the [MessageDraft] according to [SuggestedMention.offset], [SuggestedMention.replaceFrom] and
         * [SuggestedMention.target].
         *
         * The [SuggestedMention] must be up to date with the message text, that is: [SuggestedMention.replaceFrom] must
         * match the message draft at position [SuggestedMention.replaceFrom], otherwise an exception will be thrown.
         *
         * @param mention a [SuggestedMention] that can be obtained from [MessageDraftStateListener]
         * @param text the text to replace [SuggestedMention.replaceFrom] with. [SuggestedMention.replaceTo] can be used for example.
         */
        /*void insertSuggestedMention(mention: SuggestedMention, string text){
        }*/
        
        /// <summary>
        /// Add a mention to a user, channel or link specified by [target] at the given offset.
        /// </summary>
        /// <param name="offset">The start of the mention</param>
        /// <param name="length">The number of characters (length) of the mention</param>
        /// <param name="target">The target of the mention, e.g. [MentionTarget.User], [MentionTarget.Channel] or [MentionTarget.Url]</param>
        /// <exception cref="NotImplementedException"></exception>
        public void AddMention(int offset, int length, MentionTarget target)
        {
            throw new NotImplementedException();
        }

        /**
         * Remove a mention starting at the given offset, if any.
         *
         * @param offset the start of the mention to remove
         */
        public void RemoveMention(int offset)
        {
            throw new NotImplementedException();
        }

        /**
         * Update the whole message draft text with a new value.
         *
         * Internally [MessageDraft] will try to calculate the most
         * optimal set of insertions and removals that will convert the current text to the provided [text], in order to
         * preserve any mentions. This is a best effort operation, and if any mention text is found to be modified,
         * the mention will be invalidated and removed.
         */
        public void Update(string text)
        {
            throw new NotImplementedException();
        }

        /**
         * Send the [MessageDraft], along with its [files] and [quotedMessage] if any, on the [channel].
         *
         * @param meta Publish additional details with the request.
         * @param shouldStore If true, the messages are stored in Message Persistence if enabled in Admin Portal.
         * @param usePost Use HTTP POST
         * @param ttl Defines if / how long (in hours) the message should be stored in Message Persistence.
         * If shouldStore = true, and ttl = 0, the message is stored with no expiry time.
         * If shouldStore = true and ttl = X, the message is stored with an expiry time of X hours.
         * If shouldStore = false, the ttl parameter is ignored.
         * If ttl is not specified, then the expiration of the message defaults back to the expiry value for the keyset.
         *
         * @return [PNFuture] containing [PNPublishResult] that holds the timetoken of the sent message.
         */
        public async Task Send(
            Dictionary<string, object> meta = null,
            bool shouldStore = true,
            bool usePost = false,
            int ttl = -1
        )
        {
            throw new NotImplementedException();
        }

        internal override void UpdateWithPartialPtr(IntPtr partialPointer)
        {
            throw new NotImplementedException();
        }

        public override void StartListeningForUpdates()
        {
            throw new NotImplementedException();
        }

        protected override void DisposePointer()
        {
            pn_message_draft_delete(pointer);
        }
    }
}