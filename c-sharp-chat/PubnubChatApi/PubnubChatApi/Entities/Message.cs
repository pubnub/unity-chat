using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PubnubApi;
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
        /// <summary>
        /// The text content of the message.
        /// <para>
        /// This is the main content of the message. It can be any text that the user wants to send.
        /// </para>
        /// </summary>
        public string MessageText {
            get
            {
                var edits = MessageActions.Where(x => x.Type == PubnubMessageActionType.Edited).ToList();
                return edits.Any() ? edits[0].Value : OriginalMessageText;
            }
        }

        /// <summary>
        /// The original, un-edited text of the message.
        /// </summary>
        public string OriginalMessageText { get; internal set; }

        /// <summary>
        /// The time token of the message.
        /// <para>
        /// The time token is a unique identifier for the message.
        /// It is used to identify the message in the chat.
        /// </para>
        /// </summary>
        public string TimeToken { get; internal set; }

        /// <summary>
        /// The channel ID of the channel that the message belongs to.
        /// <para>
        /// This is the ID of the channel that the message was sent to.
        /// </para>
        /// </summary>
        public string ChannelId { get; internal set; }

        /// <summary>
        /// The user ID of the user that sent the message.
        /// <para>
        /// This is the unique ID of the user that sent the message.
        /// Do not confuse this with the username of the user.
        /// </para>
        /// </summary>
        public string UserId { get; internal set; }

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
        public bool IsDeleted => MessageActions.Any(x => x.Type == PubnubMessageActionType.Deleted);
        
        public List<MentionedUser> MentionedUsers {
            get
            {
                var mentioned = new List<MentionedUser>();
                if (!Meta.TryGetValue("mentionedUsers", out var rawMentionedUsers))
                {
                    return mentioned;
                }
                if (rawMentionedUsers is Dictionary<string, object> mentionedDict)
                {
                    foreach (var kvp in mentionedDict)
                    {
                        if (kvp.Value is Dictionary<string, object> mentionedUser)
                        {
                            mentioned.Add(new MentionedUser()
                            {
                                Id = (string)mentionedUser["id"],
                                Name = (string)mentionedUser["name"]
                            });
                        }
                    }
                }
                return mentioned;
            }
        }
        
        public List<ReferencedChannel> ReferencedChannels {
            get
            {
                var referenced = new List<ReferencedChannel>();
                if (!Meta.TryGetValue("referencedChannels", out var rawReferenced))
                {
                    return referenced;
                }
                if (rawReferenced is Dictionary<string, object> referencedDict)
                {
                    foreach (var kvp in referencedDict)
                    {
                        if (kvp.Value is Dictionary<string, object> referencedChannel)
                        {
                            referenced.Add(new ReferencedChannel()
                            {
                                Id = (string)referencedChannel["id"],
                                Name = (string)referencedChannel["name"]
                            });
                        }
                    }
                }
                return referenced;
            }
        }
        
        public List<TextLink> TextLinks {
            get
            {
                var links = new List<TextLink>();
                if (!Meta.TryGetValue("textLinks", out var rawLinks))
                {
                    return links;
                }
                if (rawLinks is Dictionary<string, object> linksDick)
                {
                    foreach (var kvp in linksDick)
                    {
                        if (kvp.Value is Dictionary<string, object> link)
                        {
                            links.Add(new TextLink()
                            {
                                StartIndex = Convert.ToInt32(link["start_index"]),
                                EndIndex = Convert.ToInt32(link["end_index"]),
                                Link = (string)link["link"]
                            });
                        }
                    }
                }
                return links;
            }
        }

        public List<MessageAction> MessageActions { get; internal set; } = new();

        public List<MessageAction> Reactions =>
            MessageActions.Where(x => x.Type == PubnubMessageActionType.Reaction).ToList();

        /// <summary>
        /// The data type of the message.
        /// <para>
        /// This is the type of the message data.
        /// It can be used to determine the type of the message.
        /// </para>
        /// </summary>
        /// <seealso cref="pubnub_chat_message_type"/>
        public PubnubChatMessageType Type { get; internal set; }


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

        protected override string UpdateChannelId => ChannelId;

        internal Message(Chat chat, string timeToken,string originalMessageText, string channelId, string userId, PubnubChatMessageType type, Dictionary<string, object> meta, List<MessageAction> messageActions) : base(chat, timeToken)
        {
            TimeToken = timeToken;
            OriginalMessageText = originalMessageText;
            ChannelId = channelId;
            UserId = userId;
            Type = type;
            Meta = meta;
            MessageActions = messageActions;
        }

        protected override SubscribeCallback CreateUpdateListener()
        {
            return chat.ListenerFactory.ProduceListener(messageActionCallback: delegate(Pubnub pn, PNMessageActionEventResult e)
            {
                if (ChatParsers.TryParseMessageUpdate(chat, this, e))
                {
                    OnMessageUpdated?.Invoke(this);
                }
            });
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
        public async Task<ChatOperationResult> EditMessageText(string newText)
        {
            var result = new ChatOperationResult();
            if (string.IsNullOrEmpty(newText))
            {
                result.Error = true;
                result.Exception = new PNException("Failed to edit text, new text is empty or null");
                return result;
            }
            result.RegisterOperation(await chat.PubnubInstance.AddMessageAction()
                .Action(new PNMessageAction() { Type = "edited", Value = newText })
                .Channel(ChannelId)
                .MessageTimetoken(long.Parse(TimeToken)).Channel(ChannelId).ExecuteAsync());
            return result;
        }

        public async Task<ChatOperationResult<Message>> GetQuotedMessage()
        {
            var result = new ChatOperationResult<Message>();
            if (!Meta.TryGetValue("quotedMessage", out var quotedMessage))
            {
                result.Error = true;
                result.Exception = new PNException("No quoted message was found.");
                return result;
            }
            if (quotedMessage is not Dictionary<string, object> quotedMessageDict ||
                !quotedMessageDict.TryGetValue("timetoken", out var timetoken) ||
                !quotedMessageDict.TryGetValue("channelId", out var channelId))
            {
                result.Error = true;
                result.Exception = new PNException("Quoted message data has incorrect format.");
                return result;
            }
            var getMessage = await chat.GetMessage(channelId.ToString(), timetoken.ToString());
            if (result.RegisterOperation(getMessage))
            {
                return result;
            }
            result.Result = getMessage.Result;
            return result;
        }

        public bool HasThread()
        {
            return MessageActions.Any(x => x.Type == PubnubMessageActionType.ThreadRootId);
        }

        internal string GetThreadId()
        {
            return $"{Chat.MESSAGE_THREAD_ID_PREFIX}_{ChannelId}_{TimeToken}";
        }

        public ChatOperationResult<ThreadChannel> CreateThread()
        {
            var result = new ChatOperationResult<ThreadChannel>();
            if (ChannelId.Contains(Chat.MESSAGE_THREAD_ID_PREFIX))
            {
                result.Error = true;
                result.Exception = new PNException("Only one level of thread nesting is allowed.");
                return result;
            }
            if (IsDeleted)
            {
                result.Error = true;
                result.Exception = new PNException("You cannot create threads on deleted messages.");
                return result;
            }
            if (HasThread())
            {
                result.Error = true;
                result.Exception = new PNException("Thread for this message already exist.");
                return result;
            }
            var threadId = GetThreadId();
            var description = $"Thread on message with timetoken {TimeToken} on channel {ChannelId}";
            var data = new ChatChannelData()
            {
                Description = description
            };
            result.Result = new ThreadChannel(chat, threadId, ChannelId, TimeToken, data);
            return result;
        }

        /// <summary>
        /// Asynchronously tries to get the ThreadChannel started on this Message.
        /// </summary>
        /// <returns>The retrieved ThreadChannel object, null if one wasn't found.</returns>
        public async Task<ChatOperationResult<ThreadChannel>> GetThread()
        {
            return await chat.GetThreadChannel(this);
        }

        public async Task<ChatOperationResult> RemoveThread()
        {
            var result = new ChatOperationResult();
            if (!HasThread())
            {
                result.Error = true;
                result.Exception = new PNException("There is no thread to be deleted");
                return result;
            }
            var threadMessageAction = MessageActions.First(x => x.Type == PubnubMessageActionType.ThreadRootId);
            var getThread = await GetThread();
            if (result.RegisterOperation(getThread))
            {
                return result;
            }
            if (result.RegisterOperation(await chat.PubnubInstance.RemoveMessageAction().Channel(ChannelId)
                    .MessageTimetoken(long.Parse(TimeToken)).ActionTimetoken(long.Parse(threadMessageAction.TimeToken))
                    .ExecuteAsync()))
            {
                return result;
            }
            MessageActions = MessageActions.Where(x => x.Type != PubnubMessageActionType.ThreadRootId).ToList();
            result.RegisterOperation(await getThread.Result.Delete());
            return result;
        }

        public async Task<ChatOperationResult> Pin()
        {
            var result = new ChatOperationResult();
            var getChannel = await chat.GetChannel(ChannelId);
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            result.RegisterOperation(await getChannel.Result.PinMessage(this));
            return result;
        }

        public async Task<ChatOperationResult> Report(string reason)
        {
            var jsonDict = new Dictionary<string, string>()
            {
                {"text",MessageText},
                {"reason",reason},
                {"reportedMessageChannelId",ChannelId},
                {"reportedMessageTimetoken",TimeToken},
                {"reportedUserId",UserId}
            };
            return await chat.EmitEvent(PubnubChatEventType.Report, $"{Chat.INTERNAL_MODERATION_PREFIX}_{ChannelId}",
                chat.PubnubInstance.JsonPluggableLibrary.SerializeToJsonString(jsonDict));
        }

        public async Task<ChatOperationResult> Forward(string channelId)
        {
            var result = new ChatOperationResult();
            var getChannel = await chat.GetChannel(channelId);
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            result.RegisterOperation(await getChannel.Result.ForwardMessage(this));
            return result;
        }

        public bool HasUserReaction(string reactionValue)
        {
            return Reactions.Any(x => x.Value == reactionValue);
        }

        public async Task<ChatOperationResult> ToggleReaction(string reactionValue)
        {
            var result = new ChatOperationResult();
            var currentUserId = chat.PubnubInstance.GetCurrentUserId();
            for (var i = 0; i < MessageActions.Count; i++)
            {
                var reaction = MessageActions[i];
                if (reaction.Type == PubnubMessageActionType.Reaction && reaction.UserId == currentUserId && reaction.Value == reactionValue)
                {
                    //Removing old one
                    var remove = await chat.PubnubInstance.RemoveMessageAction().MessageTimetoken(long.Parse(TimeToken))
                        .ActionTimetoken(long.Parse(reaction.TimeToken)).ExecuteAsync();
                    if (result.RegisterOperation(remove))
                    {
                        return result;
                    }
                    MessageActions.RemoveAt(i);
                    break;
                }
            }
            var add = await chat.PubnubInstance.AddMessageAction().Action(new PNMessageAction()
            {
                Type = "reaction", Value = reactionValue
            }).MessageTimetoken(long.Parse(TimeToken)).Channel(ChannelId).ExecuteAsync();
            if (result.RegisterOperation(add))
            {
                return result;
            }
            MessageActions.Add(new MessageAction()
            {
                UserId = currentUserId,
                TimeToken = add.Result.MessageTimetoken.ToString(),
                Type = PubnubMessageActionType.Reaction,
                Value = reactionValue
            });
            return result;
        }

        public async Task<ChatOperationResult> Restore()
        {
            var result = new ChatOperationResult();
            if (!IsDeleted)
            {
                result.Error = true;
                result.Exception = new PNException("Can't restore a message that wasn't deleted!");
                return result;
            }
            var deleteAction = MessageActions.First(x => x.Type == PubnubMessageActionType.Deleted);
            var restore = await chat.PubnubInstance.RemoveMessageAction().MessageTimetoken(long.Parse(TimeToken))
                .ActionTimetoken(long.Parse(deleteAction.TimeToken)).Channel(ChannelId).ExecuteAsync();
            result.RegisterOperation(restore);
            MessageActions.RemoveAt(MessageActions.IndexOf(deleteAction));
            return result;
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
        public async Task<ChatOperationResult> Delete(bool soft)
        {
            var result = new ChatOperationResult();
            if (soft)
            {
                var add = await chat.PubnubInstance.AddMessageAction()
                    .MessageTimetoken(long.Parse(TimeToken)).Action(new PNMessageAction()
                    {
                        Type = "deleted",
                        Value = "deleted"
                    }).Channel(ChannelId).ExecuteAsync();
                if (result.RegisterOperation(add))
                {
                    return result;
                }
                MessageActions.Add(new MessageAction()
                {
                    TimeToken = add.Result.ActionTimetoken.ToString(),
                    UserId = chat.PubnubInstance.GetCurrentUserId(),
                    Type = PubnubMessageActionType.Deleted,
                    Value = "deleted"
                });
            }
            else
            {
                if (HasThread())
                {
                    var getThread = await GetThread();
                    if (result.RegisterOperation(getThread))
                    {
                        return result;
                    }
                    var deleteThread = await getThread.Result.Delete();
                    if (result.RegisterOperation(deleteThread))
                    {
                        return result;
                    }
                }
                var startTimeToken = long.Parse(TimeToken) + 1;
                var deleteMessage = await chat.PubnubInstance.DeleteMessages().Start(startTimeToken)
                    .End(long.Parse(TimeToken)).ExecuteAsync();
                result.RegisterOperation(deleteMessage);
            }
            return result;
        }

        public override async Task<ChatOperationResult> Refresh()
        {
            var result = new ChatOperationResult();
            var get = await chat.GetMessage(ChannelId, TimeToken);
            if (result.RegisterOperation(get))
            {
                return result;
            }
            MessageActions = get.Result.MessageActions;
            Meta = get.Result.Meta;
            return result;
        }
    }
}