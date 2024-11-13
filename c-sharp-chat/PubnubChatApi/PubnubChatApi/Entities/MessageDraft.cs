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
    
    public class SuggestedMention {
        public int Offset;
        public string ReplaceFrom;
        public string ReplaceTo;
        MentionTarget Target;
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
        
        /// <summary>
        /// Insert mention into the MessageDraft according to SuggestedMention.Offset, SuggestedMention.ReplaceFrom and
        /// SuggestedMention.target.
        /// </summary>
        /// <param name="mention">A SuggestedMention that can be obtained from MessageDraftStateListener</param>
        /// <param name="text">The text to replace SuggestedMention.ReplaceFrom with. SuggestedMention.ReplaceTo can be used for example.</param>
        void InsertSuggestedMention(SuggestedMention mention, string text)
        {
            
        }
        
        /// <summary>
        /// Add a mention to a user, channel or link specified by [target] at the given offset.
        /// </summary>
        /// <param name="offset">The start of the mention</param>
        /// <param name="length">The number of characters (length) of the mention</param>
        /// <param name="target">The target of the mention, e.g. [MentionTarget.User], [MentionTarget.Channel] or [MentionTarget.Url]</param>
        public void AddMention(int offset, int length, MentionTarget target)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Remove a mention starting at the given offset, if any.
        /// </summary>
        /// <param name="offset">Offset the start of the mention to remove</param>
        public void RemoveMention(int offset)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Update the whole message draft text with a new value.
        /// Internally MessageDraft will try to calculate the most
        /// optimal set of insertions and removals that will convert the current text to the provided [text], in order to
        /// preserve any mentions. This is a best effort operation, and if any mention text is found to be modified,
        /// the mention will be invalidated and removed.
        /// </summary>
        /// <param name="text"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Update(string text)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Send the MessageDraft, along with its quotedMessage if any, on the channel.
        /// </summary>
        /// <param name="meta">Any additional information</param>
        /// <param name="shouldStore">If true, the messages are stored in Message Persistence if enabled in Admin Portal.</param>
        /// <param name="usePost">Use HTTP POST</param>
        /// <param name="ttl">Defines if / how long (in hours) the message should be stored in Message Persistence.
        /// If ttl is not specified, then the expiration of the message defaults back to the expiry value for the keyset.</param>
        /// <exception cref="NotImplementedException"></exception>
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