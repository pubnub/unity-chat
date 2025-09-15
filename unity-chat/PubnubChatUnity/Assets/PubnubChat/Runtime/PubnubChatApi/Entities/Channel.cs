using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using PubnubApi;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Entities.Events;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    /// <summary>
    /// Class <c>Channel</c> represents a chat channel.
    ///
    /// <para>
    /// A channel is a entity that allows users to publish and receive messages.
    /// </para>
    /// </summary>
    public class Channel : UniqueChatEntity
    {
        /// <summary>
        /// The name of the channel.
        ///
        /// <para>
        /// The name of the channel that is human meaningful.
        /// </para>
        /// </summary>
        /// <value>The name of the channel.</value>
        public string Name => channelData.Name;

        /// <summary>
        /// The description of the channel.
        ///
        /// <para>
        /// The description that allows users to understand the purpose of the channel.
        /// </para>
        public string Description => channelData.Description;

        /// <summary>
        /// The custom data of the channel.
        ///
        /// <para>
        /// The custom data that can be used to store additional information about the channel.
        /// </para>
        /// </summary>
        public Dictionary<string, object> CustomData => channelData.CustomData ?? new ();

        /// <summary>
        /// The information about the last update of the channel.
        /// <para>
        /// The time when the channel was last updated.
        /// </para>
        /// </summary>
        public string Updated => channelData.Updated;

        /// <summary>
        /// The status of the channel.
        /// <para>
        /// The last status response received from the server.
        /// </para>
        /// </summary>
        public string Status => channelData.Status;

        /// <summary>
        /// The type of the channel.
        /// <para>
        /// The type of the response received from the server when the channel was created.
        /// </para>
        /// </summary>
        public string Type => channelData.Type;

        protected ChatChannelData channelData;

        protected Subscription? subscription;
        
        private Dictionary<string, Timer> typingIndicators = new();

        /// <summary>
        /// Event that is triggered when a message is received.
        ///
        /// <para>
        /// The event is triggered when a message is received in the channel 
        /// when the channel is connected.
        /// </para>
        /// </summary>
        /// <value>The event that is triggered when a message is received.</value>
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.OnMessageReceived += (message) => {
        ///    Console.WriteLine($"Message received: {message.Text}");
        /// };
        /// channel.Connect();
        /// </code>
        /// </example>
        public event Action<Message> OnMessageReceived;

        /// <summary>
        /// Event that is triggered when the channel is updated.
        ///
        /// <para>
        /// The event is triggered when the channel is updated by the user 
        /// or by any other entity.
        /// </para>
        /// </summary>
        /// <value>The event that is triggered when the channel is updated.</value>
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.OnChannelUpdate += (channel) => {
        ///   Console.WriteLine($"Channel updated: {channel.Name}");
        /// };
        /// channel.Connect();
        /// </code>
        /// </example>
        public event Action<Channel> OnChannelUpdate;

        private Subscription presenceEventsSubscription;
        /// <summary>
        /// Event that is triggered when any presence update occurs.
        ///
        /// <para>
        /// Presence update occurs when a user joins or leaves the channel.
        /// </para>
        /// </summary>
        /// <value>The event that is triggered when any presence update occurs.</value>
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.OnPresenceUpdate += (users) => {
        ///   Console.WriteLine($"Users present: {string.Join(", ", users)}");
        /// };
        /// channel.Connect();
        /// </code>
        /// </example>
        ///
        public event Action<List<string>> OnPresenceUpdate;

        private Subscription typingEventsSubscription;
        public event Action<List<string>> OnUsersTyping;
        private Subscription readReceiptsSubscription;
        public event Action<Dictionary<string, List<string>>> OnReadReceiptEvent;
        private Subscription reportEventsSubscription;
        public event Action<ChatEvent> OnReportEvent;
        private Subscription customEventsSubscription;
        public event Action<ChatEvent> OnCustomEvent;

        protected override string UpdateChannelId => Id;

        internal Channel(Chat chat, string channelId, ChatChannelData data) : base(chat, channelId)
        {
            UpdateLocalData(data);
        }
        
        protected override SubscribeCallback CreateUpdateListener()
        {
            return chat.ListenerFactory.ProduceListener(objectEventCallback: delegate(Pubnub pn, PNObjectEventResult e)
            {
                if (ChatParsers.TryParseChannelUpdate(chat, this, e, out var updatedData))
                {
                    UpdateLocalData(updatedData);
                    OnChannelUpdate?.Invoke(this);
                }
            });
        }

        internal void UpdateLocalData(ChatChannelData? newData)
        {
            if (newData == null)
            {
                return;
            }
            channelData = newData;
        }

        internal static async Task<PNResult<PNSetChannelMetadataResult>> UpdateChannelData(Chat chat, string channelId, ChatChannelData data)
        {
            var operation = chat.PubnubInstance.SetChannelMetadata().IncludeCustom(true)
                .Channel(channelId);
            if (!string.IsNullOrEmpty(data.Name))
            {
                operation = operation.Name(data.Name);
            }
            if (!string.IsNullOrEmpty(data.Description))
            {
                operation = operation.Description(data.Description);
            }
            if (!string.IsNullOrEmpty(data.Status))
            {
                operation = operation.Status(data.Status);
            }
            if (data.CustomData != null)
            {
                operation = operation.Custom(data.CustomData);
            }
            if (!string.IsNullOrEmpty(data.Type))
            {
                operation = operation.Type(data.Type);
            }
            return await operation.ExecuteAsync().ConfigureAwait(false);
        }
        
        internal static async Task<PNResult<PNGetChannelMetadataResult>> GetChannelData(Chat chat, string channelId)
        {
            return await chat.PubnubInstance.GetChannelMetadata().IncludeCustom(true)
                .Channel(channelId)
                .ExecuteAsync().ConfigureAwait(false);
        }

        public override async Task<ChatOperationResult> Refresh()
        {
            var result = new ChatOperationResult("Channel.Refresh()", chat);
            var getData = await GetChannelData(chat, Id).ConfigureAwait(false);
            if (result.RegisterOperation(getData))
            {
                return result;
            }
            UpdateLocalData(getData.Result);
            return result;
        }

        /// <summary>
        /// Sets whether to listen for custom events on this channel.
        /// </summary>
        /// <param name="listen">True to start listening, false to stop listening.</param>
        public void SetListeningForCustomEvents(bool listen)
        {
            SetListening(ref customEventsSubscription, SubscriptionOptions.None, listen, Id, chat.ListenerFactory.ProduceListener(messageCallback:
                delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseEvent(chat, m, PubnubChatEventType.Custom, out var customEvent))
                    {
                        OnCustomEvent?.Invoke(customEvent);
                        chat.BroadcastAnyEvent(customEvent);
                    }
                }));
        }

        /// <summary>
        /// Sets whether to listen for report events on this channel.
        /// </summary>
        /// <param name="listen">True to start listening, false to stop listening.</param>
        public void SetListeningForReportEvents(bool listen)
        {
            SetListening(ref reportEventsSubscription, SubscriptionOptions.None, listen, $"{Chat.INTERNAL_MODERATION_PREFIX}_{Id}", chat.ListenerFactory.ProduceListener(messageCallback:
                delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseEvent(chat, m, PubnubChatEventType.Report, out var reportEvent))
                    {
                        OnReportEvent?.Invoke(reportEvent);
                        chat.BroadcastAnyEvent(reportEvent);
                    }
                }));
        }
        
        /// <summary>
        /// Sets whether to listen for read receipt events on this channel.
        /// </summary>
        /// <param name="listen">True to start listening, false to stop listening.</param>
        public void SetListeningForReadReceiptsEvents(bool listen)
        {
            SetListening(ref readReceiptsSubscription, SubscriptionOptions.None, listen, Id, chat.ListenerFactory.ProduceListener(messageCallback:
                async delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseEvent(chat, m, PubnubChatEventType.Receipt, out var readEvent))
                    {
                        var getMembers = await chat.GetChannelMemberships(Id).ConfigureAwait(false);
                        if (getMembers.Error)
                        {
                            return;
                        }
                        var members = getMembers.Result;
                        var outputDict = members.Memberships  
                            .GroupBy(membership => membership.LastReadMessageTimeToken)
                            .ToDictionary(  
                                g => g.Key,
                                g => g.Select(membership => membership.UserId).ToList() ?? new List<string>()
                            ) ?? new Dictionary<string, List<string>>();  
                        OnReadReceiptEvent?.Invoke(outputDict);
                        chat.BroadcastAnyEvent(readEvent);
                    }
                }));
        }

        /// <summary>
        /// Sets whether to listen for typing events on this channel.
        /// </summary>
        /// <param name="listen">True to start listening, false to stop listening.</param>
        public void SetListeningForTyping(bool listen)
        {
            SetListening(ref typingEventsSubscription, SubscriptionOptions.None, listen, Id, chat.ListenerFactory.ProduceListener(messageCallback:
                delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseEvent(chat, m, PubnubChatEventType.Typing, out var rawTypingEvent))
                    {
                        try
                        {
                            var typingEvent =
                                chat.PubnubInstance.JsonPluggableLibrary.DeserializeToDictionaryOfObject(rawTypingEvent
                                    .Payload);
                            var isTyping = (bool)typingEvent["value"];
                            var userId = rawTypingEvent.UserId;
                            
                            chat.BroadcastAnyEvent(rawTypingEvent);
                            
                            //stop typing
                            if (!isTyping)
                            {
                                if (typingIndicators.TryGetValue(userId, out var timer))
                                {
                                    timer.Stop();
                                    typingIndicators.Remove(userId);
                                    timer.Dispose();
                                }
                            }
                            //start or restart typing
                            else
                            {
                                //Stop the old timer
                                if (typingIndicators.TryGetValue(userId, out var typingTimer))
                                {
                                    typingTimer.Stop();
                                }

                                //Create and start new timer
                                var newTimer = new Timer(chat.Config.TypingTimeout);
                                newTimer.Elapsed += (_, _) =>
                                {
                                    typingIndicators.Remove(userId);
                                    OnUsersTyping?.Invoke(typingIndicators.Keys.ToList());
                                };
                                typingIndicators[userId] = newTimer;
                                newTimer.Start();
                            }
                            OnUsersTyping?.Invoke(typingIndicators.Keys.ToList());
                        }
                        catch (Exception e)
                        {
                            chat.Logger.Error($"Error when trying to broadcast typing event on channel \"{Id}\": {e.Message}");
                        }
                    }
                }));
        }

        /// <summary>
        /// Sets whether to listen for presence events on this channel.
        /// </summary>
        /// <param name="listen">True to start listening, false to stop listening.</param>
        public void SetListeningForPresence(bool listen)
        {
            SetListening(ref presenceEventsSubscription, SubscriptionOptions.ReceivePresenceEvents, listen, Id, chat.ListenerFactory.ProduceListener(presenceCallback:
                async delegate(Pubnub pn, PNPresenceEventResult p)
                {
                    var whoIs = await WhoIsPresent().ConfigureAwait(false);
                    if (whoIs.Error)
                    {
                        chat.Logger.Error($"Error when trying to broadcast presence update after WhoIs(): {whoIs.Exception.Message}");
                    }
                    else
                    {
                        OnPresenceUpdate?.Invoke(whoIs.Result);
                    }
                }));
        }

        /// <summary>
        /// Forwards a message to this channel.
        /// </summary>
        /// <param name="message">The message to forward.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> ForwardMessage(Message message)
        {
            return await SendText(message.MessageText, new SendTextParams()
            {
                Meta = message.Meta
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Emits a user mention event for this channel.
        /// </summary>
        /// <param name="userId">The ID of the user being mentioned.</param>
        /// <param name="timeToken">The time token of the message containing the mention.</param>
        /// <param name="text">The text of the mention.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public virtual async Task<ChatOperationResult> EmitUserMention(string userId, string timeToken, string text)
        {
            var jsonDict = new Dictionary<string, string>()
            {
                {"text",text},
                {"messageTimetoken",timeToken},
                {"channel",Id}
            };
            return await chat.EmitEvent(PubnubChatEventType.Mention, userId,
                chat.PubnubInstance.JsonPluggableLibrary.SerializeToJsonString(jsonDict)).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts a typing indicator for the current user in this channel.
        /// </summary>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> StartTyping()
        {
            return await chat.EmitEvent(PubnubChatEventType.Typing, Id, $"{{\"value\":true}}").ConfigureAwait(false);
        }

        /// <summary>
        /// Stops the typing indicator for the current user in this channel.
        /// </summary>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> StopTyping()
        {
            return await chat.EmitEvent(PubnubChatEventType.Typing, Id, $"{{\"value\":false}}").ConfigureAwait(false);
        }

        /// <summary>
        /// Pins a message to this channel.
        /// </summary>
        /// <param name="message">The message to pin.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> PinMessage(Message message)
        {
            channelData.CustomData ??= new ();
            channelData.CustomData["pinnedMessageChannelID"] = message.ChannelId;
            channelData.CustomData["pinnedMessageTimetoken"] = message.TimeToken;
            return (await UpdateChannelData(chat, Id, channelData).ConfigureAwait(false)).ToChatOperationResult("Channel.PinMessage()", chat);
        }

        /// <summary>
        /// Unpins the currently pinned message from this channel.
        /// </summary>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> UnpinMessage()
        {
            channelData.CustomData ??= new ();
            channelData.CustomData.Remove("pinnedMessageChannelID");
            channelData.CustomData.Remove("pinnedMessageTimetoken");
            return (await UpdateChannelData(chat, Id, channelData).ConfigureAwait(false)).ToChatOperationResult("Channel.UnPinMessage()", chat);
        }
        

        /// <summary>
        /// Asynchronously tries to get the <c>Message</c> pinned to this <c>Channel</c>.
        /// </summary>
        /// <returns>A ChatOperationResult containing the pinned Message object if there was one, null otherwise.</returns>
        public async Task<ChatOperationResult<Message>> GetPinnedMessage()
        {
            var result = new ChatOperationResult<Message>("Channel.GetPinnedMessage()", chat);
            if (result.RegisterOperation(await Refresh().ConfigureAwait(false)))
            {
                return result;
            }
            if(!CustomData.TryGetValue("pinnedMessageChannelID", out var pinnedChannelId) 
               || !CustomData.TryGetValue("pinnedMessageTimetoken", out var pinnedMessageTimeToken))
            {
                result.Error = true;
                result.Exception = new PNException($"Channel \"{Id}\" doesn't have a pinned message.");
                return result;
            }

            var getMessage = await chat.GetMessage(pinnedChannelId.ToString(), pinnedMessageTimeToken.ToString()).ConfigureAwait(false);
            if (result.RegisterOperation(getMessage))
            {
                return result;
            }

            result.Result = getMessage.Result;
            return result;
        }

        /// <summary>
        /// Creates a new MessageDraft.
        /// </summary>
        /// <param name="userSuggestionSource">Source of the user suggestions</param>
        /// <param name="isTypingIndicatorTriggered">Typing indicator trigger status.</param>
        /// <param name="userLimit">User limit.</param>
        /// <param name="channelLimit">Channel limit.</param>
        /// <param name="shouldSearchForSuggestions">Whether the MessageDraft should search for suggestions whenever the text is changed.</param>
        /// <returns>The created MessageDraft.</returns>
        public MessageDraft CreateMessageDraft(UserSuggestionSource userSuggestionSource = UserSuggestionSource.GLOBAL,
            bool isTypingIndicatorTriggered = true, int userLimit = 10, int channelLimit = 10, bool shouldSearchForSuggestions = false)
        {
            return new MessageDraft(chat, this, userSuggestionSource, isTypingIndicatorTriggered, userLimit, channelLimit, shouldSearchForSuggestions);
        }
        
        /// <summary>
        /// Disconnects from the channel.
        /// <para>
        /// Disconnects from the channel and stops receiving messages.
        /// Additionally, all the other listeners gets the presence update that the user has left the channel.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.Connect();
        /// //...
        /// channel.Disconnect();
        /// </code>
        /// </example>
        /// <seealso cref="Connect"/>
        /// <seealso cref="Join"/>
        public void Disconnect()
        {
            SetListening(ref subscription, SubscriptionOptions.None, false, Id, null);
        }

        /// <summary>
        /// Leaves the channel.
        /// <para>
        /// Leaves the channel and stops receiving messages.
        /// Additionally, all the other listeners gets the presence update that the user has left the channel.
        /// The membership is also removed from the channel.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.Join();
        /// //...
        /// channel.Leave();
        /// </code>
        /// </example>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <seealso cref="Join"/>
        /// <seealso cref="Connect"/>
        /// <seealso cref="Disconnect"/>
        public async Task<ChatOperationResult> Leave()
        {
            Disconnect();
            var currentUserId = chat.PubnubInstance.GetCurrentUserId();
            return (await chat.PubnubInstance.RemoveMemberships().Uuid(currentUserId).Include(new[]
                {
                    PNMembershipField.TYPE,
                    PNMembershipField.CUSTOM,
                    PNMembershipField.STATUS,
                    PNMembershipField.CHANNEL,
                    PNMembershipField.CHANNEL_CUSTOM,
                    PNMembershipField.CHANNEL_TYPE,
                    PNMembershipField.CHANNEL_STATUS
                }).Channels(new List<string>() { Id })
                .ExecuteAsync().ConfigureAwait(false)).ToChatOperationResult("Channel.Leave()", chat);
        }

        /// <summary>
        /// Connects to the channel.
        /// <para>
        /// Connects to the channel and starts receiving messages. 
        /// After connecting, the <see cref="OnMessageReceived"/> event is triggered when a message is received.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.OnMessageReceived += (message) => {
        ///   Console.WriteLine($"Message received: {message.Text}");
        /// };
        /// channel.Connect();
        /// </code>
        /// </example>
        /// <seealso cref="OnMessageReceived"/>
        /// <seealso cref="Disconnect"/>
        /// <seealso cref="Join"/>
        public void Connect()
        {
            SetListening(ref subscription, SubscriptionOptions.None, true, Id, chat.ListenerFactory.ProduceListener(messageCallback:
                delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseMessageResult(chat, m, out var message))
                    {
                        OnMessageReceived?.Invoke(message);
                    }
                }));
        }
        
        /// <summary>
        /// Joins the channel.
        /// <para>
        /// Joins the channel and starts receiving messages.
        /// After joining, the <see cref="OnMessageReceived"/> event is triggered when a message is received.
        /// Additionally, there is a possibility to add additional parameters to the join request.
        /// It also adds the membership to the channel.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.OnMessageReceived += (message) => {
        ///  Console.WriteLine($"Message received: {message.Text}");
        /// };
        /// channel.Join();
        /// </code>
        /// </example>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <seealso cref="OnMessageReceived"/>
        /// <seealso cref="Connect"/>
        /// <seealso cref="Disconnect"/>
        public async Task<ChatOperationResult> Join(ChatMembershipData? membershipData = null)
        {
            var result = new ChatOperationResult("Channel.Join()", chat);
            membershipData ??= new ChatMembershipData();
            var currentUserId = chat.PubnubInstance.GetCurrentUserId();
            var setMembership = await chat.PubnubInstance.SetMemberships().Uuid(currentUserId)
                .Channels(new List<PNMembership>()
                {
                    new PNMembership()
                    {
                        Channel = Id,
                        Custom = membershipData.CustomData,
                        Status = membershipData.Status,
                        Type = membershipData.Type
                    }
                })
                .Include(new []
                {
                    PNMembershipField.TYPE,
                    PNMembershipField.CUSTOM,
                    PNMembershipField.STATUS,
                    PNMembershipField.CHANNEL,
                    PNMembershipField.CHANNEL_CUSTOM
                }).ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(setMembership))
            {
                return result;
            }
            var joinMembership = new Membership(chat, currentUserId, Id, membershipData);
            var setLast = await joinMembership.SetLastReadMessageTimeToken(ChatUtils.TimeTokenNow()).ConfigureAwait(false);
            if (result.RegisterOperation(setLast))
            {
                return result;
            }
            Connect();
            return result;
        }

        /// <summary>
        /// Sets the restrictions for the user.
        /// <para>
        /// Sets the information about the restrictions for the user.
        /// The restrictions include banning and muting the user.
        /// </para>
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="banUser">if set to <c>true</c> the user is banned.</param>
        /// <param name="muteUser">if set to <c>true</c> the user is muted.</param>
        /// <param name="reason">The reason for the restrictions.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var result = await channel.SetRestrictions("user1", true, false, "Spamming");
        /// </code>
        /// </example>
        /// <seealso cref="GetUserRestrictions"/>
        public async Task<ChatOperationResult> SetRestrictions(string userId, bool banUser, bool muteUser, string reason)
        {
            return await chat.SetRestriction(userId, Id, banUser, muteUser, reason).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the restrictions for the user using a Restriction object.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="restriction">The restriction object containing ban, mute, and reason information.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> SetRestrictions(string userId, Restriction restriction)
        {
            return await SetRestrictions(userId, restriction.Ban, restriction.Mute, restriction.Reason).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the text message.
        /// <para>
        /// Sends the text message to the channel.
        /// The message is sent in the form of a text.
        /// </para>
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var result = await channel.SendText("Hello, World!");
        /// </code>
        /// </example>
        /// <seealso cref="OnMessageReceived"/>
        public async Task<ChatOperationResult> SendText(string message)
        {
            return await SendText(message, new SendTextParams()).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the text message with additional parameters.
        /// <para>
        /// Sends the text message to the channel with additional options such as metadata, quoted messages, and mentioned users.
        /// </para>
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <param name="sendTextParams">Additional parameters for sending the message.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public virtual async Task<ChatOperationResult> SendText(string message, SendTextParams sendTextParams)
        {
            var result = new ChatOperationResult("Channel.SendText()", chat);
            
            var baseInterval = Type switch
            {
                "public" => chat.Config.RateLimitsPerChannel.PublicConversation,
                "direct" => chat.Config.RateLimitsPerChannel.DirectConversation,
                "group" => chat.Config.RateLimitsPerChannel.GroupConversation,
                _ => chat.Config.RateLimitsPerChannel.UnknownConversation
            };

            TaskCompletionSource<bool> completionSource = new ();
            chat.RateLimiter.RunWithinLimits(Id, baseInterval, async () =>
            {
                var messageDict = new Dictionary<string, string>()
                {
                    {"text", message},
                    {"type", "text"}
                };
                var meta = sendTextParams.Meta ?? new Dictionary<string, object>();
                if (sendTextParams.QuotedMessage != null)
                {
                    //TODO: may create some "ToJSON()" methods for chat entities
                    //TODO: what about edited messages??
                    meta.Add("quotedMessage", new Dictionary<string, string>()
                    {
                        {"timetoken", sendTextParams.QuotedMessage.TimeToken},
                        {"text", sendTextParams.QuotedMessage.OriginalMessageText},
                        {"userId", sendTextParams.QuotedMessage.UserId},
                        {"channelId", sendTextParams.QuotedMessage.ChannelId},
                    });
                }
                if (sendTextParams.MentionedUsers.Any())
                {
                    meta.Add("mentionedUsers", sendTextParams.MentionedUsers);
                }

                var publishResult = await chat.PubnubInstance.Publish()
                    .Channel(Id)
                    .ShouldStore(sendTextParams.StoreInHistory)
                    .UsePOST(sendTextParams.SendByPost)
                    .Message(chat.PubnubInstance.JsonPluggableLibrary.SerializeToJsonString(messageDict))
                    .Meta(meta)
                    .ExecuteAsync().ConfigureAwait(false);
                if (result.RegisterOperation(publishResult))
                {
                    return result;
                }
                foreach (var mention in sendTextParams.MentionedUsers)
                {
                    result.RegisterOperation(await EmitUserMention(mention.Value.Id,
                        publishResult.Result.Timetoken.ToString(), message).ConfigureAwait(false));
                }
                return result;
            }, response =>
            {
                if (result.Error)
                {
                    chat.Logger.Error($"Error occured when trying to SendText(): {result.Exception.Message}");
                }
                completionSource.SetResult(true);
            }, exception =>
            {
                chat.Logger.Error($"Error occured when trying to SendText(): {exception.Message}");
                completionSource.SetResult(true);
            });

            await completionSource.Task.ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Updates the channel.
        /// <para>
        /// Updates the channel with the new data.
        /// The data includes the name, description, custom data, and type of the channel.
        /// </para>
        /// </summary>
        /// <param name="updatedData">The updated data of the channel.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var result = await channel.Update(new ChatChannelData {
        ///  Name = "newName",
        ///  Description = "newDescription",
        ///  CustomDataJson = "{\"key\": \"value\"}",
        ///  Type = "newType"
        /// });
        /// </code>
        /// </example>
        /// <seealso cref="OnChannelUpdate"/>
        /// <seealso cref="ChatChannelData"/>
        public async Task<ChatOperationResult> Update(ChatChannelData updatedData)
        {
            return await chat.UpdateChannel(Id, updatedData).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the channel.
        /// <para>
        /// Deletes the channel and removes all the messages and memberships from the channel.
        /// </para>
        /// </summary>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var result = await channel.Delete();
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> Delete()
        {
            return await chat.DeleteChannel(Id).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the user restrictions for a specific user.
        /// <para>
        /// Gets the user restrictions that include the information about the bans and mutes for the specified user.
        /// </para>
        /// </summary>
        /// <param name="user">The user to get restrictions for.</param>
        /// <returns>A ChatOperationResult containing the Restriction object if restrictions exist for the user, error otherwise.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var user = //...
        /// var result = await channel.GetUserRestrictions(user);
        /// var restriction = result.Result;
        /// </code>
        /// </example>
        /// <seealso cref="SetRestrictions"/>
        public async Task<ChatOperationResult<Restriction>> GetUserRestrictions(User user)
        {
            var result = new ChatOperationResult<Restriction>("Channel.GetUserRestrictions()", chat);
            var membershipsResult = await chat.PubnubInstance.GetMemberships().Uuid(user.Id).Include(new[]
            {
                PNMembershipField.CUSTOM
            }).Filter($"channel.id == \"{Chat.INTERNAL_MODERATION_PREFIX}_{Id}\"").IncludeCount(true).ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(membershipsResult) || membershipsResult.Result.Memberships == null || !membershipsResult.Result.Memberships.Any())
            {
                result.Error = true;
                return result;
            }
            var membership = membershipsResult.Result.Memberships[0];
            try
            {
                result.Result = new Restriction()
                {
                    Ban = (bool)membership.Custom["ban"],
                    Mute = (bool)membership.Custom["mute"],
                    Reason = (string)membership.Custom["reason"]
                };
            }
            catch (Exception e)
            {
                result.Error = true;
                result.Exception = e;
            }
            return result;
        }

        /// <summary>
        /// Gets all user restrictions for this channel.
        /// </summary>
        /// <param name="sort">Sort criteria for restrictions.</param>
        /// <param name="limit">The maximum number of restrictions to retrieve.</param>
        /// <param name="page">Pagination object for retrieving specific page results.</param>
        /// <returns>A ChatOperationResult containing the wrapper with all user restrictions for this channel.</returns>
        public async Task<ChatOperationResult<UsersRestrictionsWrapper>> GetUsersRestrictions(string sort = "", int limit = 0,
            PNPageObject page = null)
        {
            var result = new ChatOperationResult<UsersRestrictionsWrapper>("Channel.GetUsersRestrictions()", chat){Result = new UsersRestrictionsWrapper()};
            var operation = chat.PubnubInstance.GetChannelMembers().Channel($"{Chat.INTERNAL_MODERATION_PREFIX}_{Id}")
                .Include(new[]
                {
                    PNChannelMemberField.CUSTOM,
                    PNChannelMemberField.UUID
                }).IncludeCount(true);
            if (!string.IsNullOrEmpty(sort))
            {
                operation = operation.Sort(new List<string>() { sort });
            }
            if (limit > 0)
            {
                operation = operation.Limit(limit);
            }
            if (page != null)
            {
                operation = operation.Page(page);
            }
            var membersResult = await operation.ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(membersResult))
            {
                return result;
            }

            result.Result.Page = membersResult.Result.Page;
            result.Result.Total = membersResult.Result.TotalCount;
            foreach (var member in membersResult.Result.ChannelMembers)
            {
                try
                {
                    result.Result.Restrictions.Add(new UserRestriction()
                    {
                        Ban = (bool)member.Custom["ban"],
                        Mute = (bool)member.Custom["mute"],
                        Reason = (string)member.Custom["reason"],
                        UserId = member.UuidMetadata.Uuid
                    });
                }
                catch (Exception e)
                {
                    chat.Logger.Warn($"Incorrect data was encountered when parsing Channel Restriction for User \"{member.UuidMetadata.Uuid}\" in Channel \"{Id}\". Exception was: {e.Message}");
                }
            }
            return result;
        }

        /// <summary>
        /// Determines whether the user is present in the channel.
        /// <para>
        /// The method checks whether the user is present in the channel.
        /// </para>
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>A ChatOperationResult containing <c>true</c> if the user is present in the channel; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var result = await channel.IsUserPresent("user1");
        /// var isUserPresent = result.Result;
        /// Console.WriteLine($"User present: {isUserPresent}");
        /// </code>
        /// </example>
        /// <seealso cref="WhoIsPresent"/>
        public async Task<ChatOperationResult<bool>> IsUserPresent(string userId)
        {
            var result = new ChatOperationResult<bool>("Channel.IsUserPresent()", chat);
            var wherePresent = await chat.WherePresent(userId).ConfigureAwait(false);
            if (result.RegisterOperation(wherePresent))
            {
                return result;
            }
            result.Result = wherePresent.Result.Contains(Id);
            return result;
        }

        /// <summary>
        /// Gets the list of users present in the channel.
        /// <para>
        /// Gets all the users that are present in the channel.
        /// </para>
        /// </summary>
        /// <returns>A ChatOperationResult containing the list of users present in the channel.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var result = await channel.WhoIsPresent();
        /// var users = result.Result;
        /// foreach (var user in users) {
        ///  Console.WriteLine($"User present: {user}");
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="IsUserPresent"/>
        public async Task<ChatOperationResult<List<string>>> WhoIsPresent()
        {
            var result = new ChatOperationResult<List<string>>("Channel.WhoIsPresent()", chat) { Result = new List<string>() };
            var response = await chat.PubnubInstance.HereNow().Channels(new[] { Id }).IncludeState(true)
                .IncludeUUIDs(true).ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(response))
            {
                return result;
            }

            foreach (var occupant in response.Result.Channels[Id].Occupants)
            {
                result.Result.Add(occupant.Uuid);
            }
            return result;
        }

        /// <summary>
        /// Gets the list of the <c>Membership</c> objects.
        /// <para>
        /// Gets the list of the <c>Membership</c> objects that represent the users that are members 
        /// of the channel and the relationships between the users and the channel.
        /// </para>
        /// </summary>
        /// <param name="filter">The filter parameter.</param>
        /// <param name="sort">The sort parameter.</param>
        /// <param name="limit">The maximum amount of the memberships received.</param>
        /// <param name="page">The page object for pagination.</param>
        /// <returns>A ChatOperationResult containing the list of the <c>Membership</c> objects.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var result = await channel.GetMemberships(limit: 10);
        /// var memberships = result.Result.Memberships;
        /// foreach (var membership in memberships) {
        ///   Console.WriteLine($"Membership: {membership.UserId}");
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="Membership"/>
        public async Task<ChatOperationResult<MembersResponseWrapper>> GetMemberships(string filter = "", string sort = "", int limit = 0,
            PNPageObject page = null)
        {
            return await chat.GetChannelMemberships(Id, filter, sort, limit, page).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously gets the <c>Message</c> object for the given timetoken sent from this <c>Channel</c>.
        /// </summary>
        /// <param name="timeToken">TimeToken of the searched-for message.</param>
        /// <returns>A ChatOperationResult containing the Message object if one was found, null otherwise.</returns>
        public async Task<ChatOperationResult<Message>> GetMessage(string timeToken)
        {
            return await chat.GetMessage(Id, timeToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the message history for this channel within a specified time range.
        /// </summary>
        /// <param name="startTimeToken">The start time token for the history range.</param>
        /// <param name="endTimeToken">The end time token for the history range.</param>
        /// <param name="count">The maximum number of messages to retrieve.</param>
        /// <returns>A ChatOperationResult containing the list of messages from this channel.</returns>
        public async Task<ChatOperationResult<List<Message>>> GetMessageHistory(string startTimeToken, string endTimeToken,
            int count)
        {
            return await chat.GetChannelMessageHistory(Id, startTimeToken, endTimeToken, count).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Invites a user to this channel.
        /// </summary>
        /// <param name="user">The user to invite.</param>
        /// <returns>A ChatOperationResult containing the created membership for the invited user.</returns>
        public async Task<ChatOperationResult<Membership>> Invite(User user)
        {
            return await chat.InviteToChannel(Id, user.Id).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Invites multiple users to this channel.
        /// </summary>
        /// <param name="users">The list of users to invite.</param>
        /// <returns>A ChatOperationResult containing a list of created memberships for the invited users.</returns>
        public async Task<ChatOperationResult<List<Membership>>> InviteMultiple(List<User> users)
        {
            return await chat.InviteMultipleToChannel(Id, users).ConfigureAwait(false);
        }
    }
}