using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PubnubChatApi.Entities.Data;
using DiffMatchPatch;

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
    }

    /// <summary>
    /// Internal class to track mentions within the draft text
    /// </summary>
    internal class InternalMention
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public MentionTarget Target { get; set; }

        public int EndExclusive => Start + Length;

        public InternalMention(int start, int length, MentionTarget target)
        {
            Start = start;
            Length = length;
            Target = target;
        }
    }

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
        
        // Static regex patterns for mention detection
        private static readonly Regex UserMentionRegex = new Regex(@"((?=\s?)@[a-zA-Z0-9_]+)", RegexOptions.Compiled);
        private static readonly Regex ChannelReferenceRegex = new Regex(@"((?=\s?)#[a-zA-Z0-9_]+)", RegexOptions.Compiled);
        
        // Schema prefixes for rendering mentions
        private static readonly string SchemaUser = "pn-user://";
        private static readonly string SchemaChannel = "pn-channel://";
        
        // Internal state
        private string _value = string.Empty;
        private List<InternalMention> _mentions = new ();
        private diff_match_patch _diffMatchPatch = new ();
        
        public event Action<List<MessageElement>, List<SuggestedMention>> OnDraftUpdated;
        
        /// <summary>
        /// Gets the current text of the draft
        /// </summary>
        public string Text => _value;
        
        /// <summary>
        /// Gets the current message elements
        /// </summary>
        public List<MessageElement> MessageElements => GetMessageElements();

        public bool ShouldSearchForSuggestions { get; set; }
        
        private Channel channel;
        private Chat chat;
        
        private bool isTypingIndicatorTriggered;
        private UserSuggestionSource userSuggestionSource;
        private int userLimit;
        private int channelLimit;

        internal MessageDraft(Chat chat, Channel channel, UserSuggestionSource userSuggestionSource, bool isTypingIndicatorTriggered, int userLimit, int channelLimit, bool shouldSearchForSuggestions)
        {
            this.chat = chat;
            this.channel = channel;
            this.isTypingIndicatorTriggered = isTypingIndicatorTriggered;
            this.userSuggestionSource = userSuggestionSource;
            this.userLimit = userLimit;
            this.channelLimit = channelLimit;
            ShouldSearchForSuggestions = shouldSearchForSuggestions;
        }

        private async void BroadcastDraftUpdate()
        {
            try
            {
                var messageElements = GetMessageElements();
                var suggestedMentions = ShouldSearchForSuggestions ? await GenerateSuggestedMentions().ConfigureAwait(false) : new List<SuggestedMention>();
                OnDraftUpdated?.Invoke(messageElements, suggestedMentions);
            }
            catch (Exception e)
            {
                chat.Logger.Error($"Error has occured when trying to broadcast MessageDraft update: {e.Message}");
            }
        }

        /// <summary>
        /// Generates suggested mentions based on current text patterns
        /// </summary>
        private async Task<List<SuggestedMention>> GenerateSuggestedMentions()
        {
            var suggestions = new List<SuggestedMention>();
            var rawMentions = SuggestRawMentions();

            foreach (var rawMention in rawMentions)
            {
                var suggestion = new SuggestedMention
                {
                    Offset = rawMention.Start,
                    ReplaceFrom = _value.Substring(rawMention.Start, rawMention.Length),
                };
                switch (rawMention.Target.Type)
                {
                    case MentionType.User:
                        var usersWrapper =
                            await chat.GetUsers(filter: $"name LIKE \"{rawMention.Target.Target}*\"", limit:userLimit).ConfigureAwait(false);   
                        if (!usersWrapper.Error && usersWrapper.Result.Users.Any())
                        {
                            var user = usersWrapper.Result.Users[0];
                            suggestion.Target = new MentionTarget() { Target = user.Id, Type = rawMention.Target.Type };
                            suggestion.ReplaceTo = user.UserName;
                            if (userSuggestionSource == UserSuggestionSource.CHANNEL &&
                                !(await user.IsPresentOn(channel.Id).ConfigureAwait(false)).Result)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                        break;
                    case MentionType.Channel:
                        var channelsWrapper = await chat.GetChannels(filter: $"name LIKE \"{rawMention.Target.Target}*\"",
                            limit: channelLimit).ConfigureAwait(false);
                        if (channelsWrapper.Channels != null && channelsWrapper.Channels.Any())
                        {
                            var mentionedChannel = channelsWrapper.Channels[0];
                            suggestion.Target = new MentionTarget() { Target = channel.Id, Type = rawMention.Target.Type };
                            suggestion.ReplaceTo = mentionedChannel.Name;
                        }
                        else
                        {
                            continue;
                        }
                        break;
                    case MentionType.Url:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                suggestions.Add(suggestion);
            }

            return suggestions;
        }
        
        /// <summary>
        /// Suggests raw mentions based on regex patterns in the text
        /// </summary>
        internal List<InternalMention> SuggestRawMentions()
        {
            var allMentions = new List<InternalMention>();

            // Find user mentions (@username)
            var userMatches = UserMentionRegex.Matches(_value);
            foreach (Match match in userMatches)
            {
                bool alreadyMentioned = _mentions.Any(mention => mention.Start == match.Index);
                if (!alreadyMentioned)
                {
                    var target = new MentionTarget { Type = MentionType.User, Target = match.Value[1..] };
                    allMentions.Add(new InternalMention(match.Index, match.Length, target));
                }
            }

            // Find channel mentions (#channel)
            var channelMatches = ChannelReferenceRegex.Matches(_value);
            foreach (Match match in channelMatches)
            {
                bool alreadyMentioned = _mentions.Any(mention => mention.Start == match.Index);
                if (!alreadyMentioned)
                {
                    var target = new MentionTarget { Type = MentionType.Channel, Target = match.Value[1..] };
                    allMentions.Add(new InternalMention(match.Index, match.Length, target));
                }
            }

            // Sort by start position
            allMentions.Sort((a, b) => a.Start.CompareTo(b.Start));

            return allMentions;
        }
        
        /// <summary>
        /// Inserts a suggested mention into the draft at the appropriate position.
        /// <para>
        /// Insert mention into the MessageDraft according to SuggestedMention.Offset, SuggestedMention.ReplaceFrom and
        /// SuggestedMention.target.
        /// </para>
        /// </summary>
        /// <param name="mention">A SuggestedMention that can be obtained from OnDraftUpdated when ShouldSearchForSuggestions is set to true</param>
        /// <param name="text">The text to replace SuggestedMention.ReplaceFrom with. SuggestedMention.ReplaceTo can be used for example.</param>
        public void InsertSuggestedMention(SuggestedMention mention, string text)
        {
            if (mention == null || string.IsNullOrEmpty(text) || mention.Target == null) return;
            if (!ValidateSuggestedMention(mention)) return;
            
            TriggerTypingIndicator();

            // Remove the text that should be replaced
            ApplyRemoveTextInternal(mention.Offset, mention.ReplaceFrom.Length);
            
            // Insert the new text
            ApplyInsertTextInternal(mention.Offset, text);
            
            // Add mention for the inserted text
            _mentions.Add(new InternalMention(mention.Offset, text.Length, mention.Target));
            
            // Sort mentions by start position
            _mentions.Sort((a, b) => a.Start.CompareTo(b.Start));

            BroadcastDraftUpdate();
        }
        
        /// <summary>
        /// Insert some text into the MessageDraft text at the given offset.
        /// </summary>
        /// <param name="offset">The position from the start of the message draft where insertion will occur</param>
        /// <param name="text">Text the text to insert at the given offset</param>
        public void InsertText(int offset, string text)
        {
            if (string.IsNullOrEmpty(text) || offset < 0 || offset > _value.Length)
            {
                return;
            }

            TriggerTypingIndicator();

            // Insert text at the specified position
            _value = _value.Insert(offset, text);

            // Filter out mentions that overlap with the insertion point and adjust positions
            var newMentions = new List<InternalMention>();
            
            foreach (var mention in _mentions)
            {
                // Only keep mentions that don't overlap with the insertion point
                if (offset <= mention.Start || offset >= mention.EndExclusive)
                {
                    var newMention = new InternalMention(mention.Start, mention.Length, mention.Target);
                    
                    // Adjust start position if the mention comes after the insertion point
                    if (offset <= mention.Start)
                    {
                        newMention.Start += text.Length;
                    }
                    
                    newMentions.Add(newMention);
                }
            }
            
            _mentions = newMentions;
            BroadcastDraftUpdate();
        }

        /// <summary>
        /// Remove a number of characters from the MessageDraft text at the given offset.
        /// </summary>
        /// <param name="offset">The position from the start of the message draft where removal will occur</param>
        /// <param name="length">Length the number of characters to remove, starting at the given offset</param>
        public void RemoveText(int offset, int length)
        {
            if (offset < 0 || offset >= _value.Length || length <= 0)
            {
                return;
            }

            TriggerTypingIndicator();
            
            // Clamp length to not exceed the text bounds
            length = Math.Min(length, _value.Length - offset);

            // Remove text from the specified position
            _value = _value.Remove(offset, length);

            // Filter out mentions that overlap with the removal range and adjust positions
            var newMentions = new List<InternalMention>();
            
            foreach (var mention in _mentions)
            {
                // Only keep mentions that don't overlap with the removal range
                if (offset > mention.EndExclusive || offset + length <= mention.Start)
                {
                    var newMention = new InternalMention(mention.Start, mention.Length, mention.Target);
                    
                    // Adjust start position if the mention comes after the removal range
                    if (offset < mention.Start)
                    {
                        newMention.Start -= Math.Min(length, mention.Start - offset);
                    }
                    
                    newMentions.Add(newMention);
                }
            }
            
            _mentions = newMentions;
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
            if (target == null || offset < 0 || length <= 0 || offset + length > _value.Length) return;

            // Add the mention to the list
            _mentions.Add(new InternalMention(offset, length, target));

            // Sort mentions by start position
            _mentions.Sort((a, b) => a.Start.CompareTo(b.Start));

            BroadcastDraftUpdate();
        }

        /// <summary>
        /// Remove a mention starting at the given offset, if any.
        /// </summary>
        /// <param name="offset">Offset the start of the mention to remove</param>
        public void RemoveMention(int offset)
        {
            // Remove mentions that start at the specified offset
            _mentions.RemoveAll(mention => mention.Start == offset);

            // Sort mentions by start position
            _mentions.Sort((a, b) => a.Start.CompareTo(b.Start));

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
            if (text == null) text = string.Empty;
            
            TriggerTypingIndicator();

            // Use diff-match-patch to compute differences
            var diffs = _diffMatchPatch.diff_main(_value, text);
            _diffMatchPatch.diff_cleanupSemantic(diffs);

            int consumed = 0;

            // Apply each diff operation
            foreach (var diff in diffs)
            {
                switch (diff.operation)
                {
                    case Operation.DELETE:
                        // Apply removal without broadcasting
                        ApplyRemoveTextInternal(consumed, diff.text.Length);
                        break;

                    case Operation.INSERT:
                        // Apply insertion without broadcasting
                        ApplyInsertTextInternal(consumed, diff.text);
                        consumed += diff.text.Length;
                        break;

                    case Operation.EQUAL:
                        consumed += diff.text.Length;
                        break;
                }
            }

            BroadcastDraftUpdate();
        }

        /// <summary>
        /// Internal method to apply text insertion without triggering broadcasts
        /// </summary>
        private void ApplyInsertTextInternal(int offset, string text)
        {
            if (text == null) text = string.Empty;
            if (offset < 0 || offset > _value.Length) return;

            // Insert text at the specified position
            _value = _value.Insert(offset, text);

            // Filter out mentions that overlap with the insertion point and adjust positions
            var newMentions = new List<InternalMention>();
            
            foreach (var mention in _mentions)
            {
                // Only keep mentions that don't overlap with the insertion point
                if (offset <= mention.Start || offset >= mention.EndExclusive)
                {
                    var newMention = new InternalMention(mention.Start, mention.Length, mention.Target);
                    
                    // Adjust start position if the mention comes after the insertion point
                    if (offset <= mention.Start)
                    {
                        newMention.Start += text.Length;
                    }
                    
                    newMentions.Add(newMention);
                }
            }
            
            _mentions = newMentions;
        }

        /// <summary>
        /// Internal method to apply text removal without triggering broadcasts
        /// </summary>
        private void ApplyRemoveTextInternal(int offset, int length)
        {
            if (offset < 0 || offset >= _value.Length || length <= 0) return;
            
            // Clamp length to not exceed the text bounds
            length = Math.Min(length, _value.Length - offset);

            // Remove text from the specified position
            _value = _value.Remove(offset, length);

            // Filter out mentions that overlap with the removal range and adjust positions
            var newMentions = new List<InternalMention>();
            
            foreach (var mention in _mentions)
            {
                // Only keep mentions that don't overlap with the removal range
                if (offset > mention.EndExclusive || offset + length <= mention.Start)
                {
                    var newMention = new InternalMention(mention.Start, mention.Length, mention.Target);
                    
                    // Adjust start position if the mention comes after the removal range
                    if (offset < mention.Start)
                    {
                        newMention.Start -= Math.Min(length, mention.Start - offset);
                    }
                    
                    newMentions.Add(newMention);
                }
            }
            
            _mentions = newMentions;
        }

        /// <summary>
        /// Send the MessageDraft, along with its quotedMessage if any, on the channel.
        /// </summary>
        public async Task<ChatOperationResult> Send()
        {
            return await Send(new SendTextParams()).ConfigureAwait(false);
        }

        /// <summary>
        /// Send the rendered MessageDraft on the channel.
        /// </summary>
        /// <param name="sendTextParams">Additional parameters for sending the message.</param>
        public async Task<ChatOperationResult> Send(SendTextParams sendTextParams)
        {
            var mentions = new Dictionary<int, MentionedUser>();
            //TODO: revisit if this is the final data format and how to solve that we don't include name anywhere
            var userMentionIndex = 0;
            var channelReferenceIndex = 0;
            var textLinkIndex = 0;
            foreach (var internalMention in _mentions)
            {
                switch (internalMention.Target.Type)
                {
                    case MentionType.User:
                        mentions.Add(userMentionIndex++, new MentionedUser(){Id = internalMention.Target.Target});
                        break;
                    case MentionType.Channel:
                        var reference = new ReferencedChannel() { Id = internalMention.Target.Target };
                        if (sendTextParams.Meta.TryGetValue("referencedChannels", out var refs))
                        {
                            if (refs is Dictionary<int, object> referencedChannels)
                            {
                                referencedChannels.Add(channelReferenceIndex++, reference);
                            }
                        }
                        else
                        {
                            sendTextParams.Meta.Add("referencedChannels", new Dictionary<int, object>(){{channelReferenceIndex++, reference}});
                        }
                        break;
                    case MentionType.Url:
                        var link = new TextLink() { StartIndex = internalMention.Start, EndIndex = internalMention.EndExclusive, Link = internalMention.Target.Target };
                        if (sendTextParams.Meta.TryGetValue("textLinks", out var linkObjects))
                        {
                            if (linkObjects is Dictionary<int, object> links)
                            {
                                links.Add(textLinkIndex++, link);
                            }
                        }
                        else
                        {
                            sendTextParams.Meta.Add("textLinks", new Dictionary<int, object>(){{textLinkIndex++, link}});
                        }
                        break;
                    default:
                        break;
                }
            }
            sendTextParams.MentionedUsers = mentions;
            return await channel.SendText(Render(), sendTextParams).ConfigureAwait(false);
        }

        /// <summary>
        /// Validates that a suggested mention is valid for the current text
        /// </summary>
        private bool ValidateSuggestedMention(SuggestedMention suggestedMention)
        {
            if (suggestedMention.Offset < 0 || suggestedMention.Offset >= _value.Length) return false;
            if (string.IsNullOrEmpty(suggestedMention.ReplaceFrom)) return false;
            if (suggestedMention.Offset + suggestedMention.ReplaceFrom.Length > _value.Length) return false;

            var substring = _value.Substring(suggestedMention.Offset, suggestedMention.ReplaceFrom.Length);
            return substring == suggestedMention.ReplaceFrom;
        }

        /// <summary>
        /// Validates that mentions don't overlap and are within valid text bounds.
        /// </summary>
        /// <returns>True if all mentions are valid, false otherwise.</returns>
        public bool ValidateMentions()
        {
            for (int i = 0; i < _mentions.Count; i++)
            {
                if (i > 0 && _mentions[i].Start < _mentions[i - 1].EndExclusive)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets message elements with plain text and links
        /// </summary>
        public List<MessageElement> GetMessageElements()
        {
            var elements = new List<MessageElement>();
            int lastPosition = 0;

            foreach (var mention in _mentions)
            {
                // Add plain text before the mention
                if (lastPosition < mention.Start)
                {
                    var plainText = _value.Substring(lastPosition, mention.Start - lastPosition);
                    if (!string.IsNullOrEmpty(plainText))
                    {
                        elements.Add(new MessageElement { Text = plainText, MentionTarget = null });
                    }
                }

                // Add the mention element
                var mentionText = _value.Substring(mention.Start, mention.Length);
                elements.Add(new MessageElement { Text = mentionText, MentionTarget = mention.Target });

                lastPosition = mention.EndExclusive;
            }

            // Add remaining text after last mention
            if (lastPosition < _value.Length)
            {
                var remainingText = _value.Substring(lastPosition);
                if (!string.IsNullOrEmpty(remainingText))
                {
                    elements.Add(new MessageElement { Text = remainingText, MentionTarget = null });
                }
            }

            return elements;
        }

        /// <summary>
        /// Renders the draft text with mentions converted to their appropriate schema format.
        /// <para>
        /// Renders the message with markdown-style links.
        /// </para>
        /// </summary>
        /// <returns>The rendered text with schema-formatted mentions.</returns>
        public string Render()
        {
            var elements = GetMessageElements();
            var result = new System.Text.StringBuilder();

            foreach (var element in elements)
            {
                if (element.MentionTarget == null)
                {
                    result.Append(element.Text);
                }
                else
                {
                    var escapedText = EscapeLinkText(element.Text);
                    var escapedUrl = EscapeLinkUrl(element.MentionTarget.Target);

                    switch (element.MentionTarget.Type)
                    {
                        case MentionType.User:
                            result.Append($"[{escapedText}]({SchemaUser}{escapedUrl})");
                            break;
                        case MentionType.Channel:
                            result.Append($"[{escapedText}]({SchemaChannel}{escapedUrl})");
                            break;
                        case MentionType.Url:
                            result.Append($"[{escapedText}]({escapedUrl})");
                            break;
                    }
                }
            }

            return result.ToString();
        }

        private async void TriggerTypingIndicator()
        {
            if (isTypingIndicatorTriggered && channel.Type == "public")
            {
                await channel.StartTyping().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Escapes text for use in markdown links
        /// </summary>
        private static string EscapeLinkText(string text)
        {
            return text?.Replace("\\", "\\\\").Replace("]", "\\]") ?? string.Empty;
        }

        /// <summary>
        /// Escapes URLs for use in markdown links
        /// </summary>
        private static string EscapeLinkUrl(string url)
        {
            return url?.Replace("\\", "\\\\").Replace(")", "\\)") ?? string.Empty;
        }
    }
}