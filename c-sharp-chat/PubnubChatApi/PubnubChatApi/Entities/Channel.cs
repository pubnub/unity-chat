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
        public string Name => channelData.ChannelName;

        /// <summary>
        /// The description of the channel.
        ///
        /// <para>
        /// The description that allows users to understand the purpose of the channel.
        /// </para>
        public string Description => channelData.ChannelDescription;

        /// <summary>
        /// The custom data of the channel.
        ///
        /// <para>
        /// The custom data that can be used to store additional information about the channel.
        /// </para>
        /// </summary>
        public Dictionary<string, object> CustomData => channelData.ChannelCustomData;

        /// <summary>
        /// The information about the last update of the channel.
        /// <para>
        /// The time when the channel was last updated.
        /// </para>
        /// </summary>
        public string Updated => channelData.ChannelUpdated;

        /// <summary>
        /// The status of the channel.
        /// <para>
        /// The last status response received from the server.
        /// </para>
        /// </summary>
        public string Status => channelData.ChannelStatus;

        /// <summary>
        /// The type of the channel.
        /// <para>
        /// The type of the response received from the server when the channel was created.
        /// </para>
        /// </summary>
        public string Type => channelData.ChannelType;

        private ChatChannelData channelData;

        protected Chat chat;

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

        public event Action<List<string>> OnUsersTyping;

        public event Action<Dictionary<string, List<string>?>> OnReadReceiptEvent;
        public event Action<ChatEvent> OnReportEvent;
        public event Action<ChatEvent> OnCustomEvent;

        internal Channel(Chat chat, string channelId, ChatChannelData data) : base(channelId)
        {
            this.chat = chat;
            UpdateLocalData(data);
        }

        internal void UpdateLocalData(ChatChannelData? newData)
        {
            if (newData == null)
            {
                return;
            }
            channelData = newData;
        }

        internal static async Task<bool> UpdateChannelData(Chat chat, string channelId, ChatChannelData data)
        {
            var result = await chat.PubnubInstance.SetChannelMetadata().IncludeCustom(true)
                .Channel(channelId)
                .Name(data.ChannelName)
                .Description(data.ChannelDescription)
                .Status(data.ChannelStatus)
                .Custom(data.ChannelCustomData)
                .ExecuteAsync();
            if (result.Status.Error)
            {
                chat.Logger.Error($"Error when trying to set data for channel \"{channelId}\": {result.Status.ErrorData.Information}");
                return false;
            }
            return true;
        }
        
        internal static async Task<ChatChannelData?> GetChannelData(Chat chat, string channelId)
        {
            var result = await chat.PubnubInstance.GetChannelMetadata().IncludeCustom(true)
                .Channel(channelId)
                .ExecuteAsync();
            if (result.Status.Error)
            {
                chat.Logger.Error($"Error when trying to get data for channel \"{channelId}\": {result.Status.ErrorData.Information}");
                return null;
            }
            try
            {
                return (ChatChannelData)result.Result;
            }
            catch (Exception e)
            {
                chat.PubnubInstance.PNConfig.Logger.Error($"Error when trying to parse data for Channel \"{channelId}\": {e.Message}");
                return null;
            }
        }

        public override async Task Resync()
        {
            var newData = await GetChannelData(chat, Id);
            UpdateLocalData(newData);
        }

        public async void SetListeningForCustomEvents(bool listen)
        {
            throw new NotImplementedException();
        }

        internal void BroadcastCustomEvent(ChatEvent chatEvent)
        {
            OnCustomEvent?.Invoke(chatEvent);
        }

        public async void SetListeningForReportEvents(bool listen)
        {
            throw new NotImplementedException();
        }

        internal void BroadcastReportEvent(ChatEvent chatEvent)
        {
            OnReportEvent?.Invoke(chatEvent);
        }

        public async void SetListeningForReadReceiptsEvents(bool listen)
        {
            throw new NotImplementedException();
        }

        public async void SetListeningForTyping(bool listen)
        {
            throw new NotImplementedException();
        }

        public async void SetListeningForPresence(bool listen)
        {
            throw new NotImplementedException();
        }
        
        internal void BroadcastMessageReceived(Message message)
        {
            OnMessageReceived?.Invoke(message);
        }

        internal void BroadcastReadReceipt(Dictionary<string, List<string>?> readReceiptEventData)
        {
            OnReadReceiptEvent?.Invoke(readReceiptEventData);
        }

        internal void BroadcastChannelUpdate()
        {
            OnChannelUpdate?.Invoke(this);
        }

        internal async void BroadcastPresenceUpdate()
        {
            OnPresenceUpdate?.Invoke(await WhoIsPresent());
        }

        internal void TryParseAndBroadcastTypingEvent(List<string> userIds)
        {
            //stop typing
            var keys = typingIndicators.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var indicator = typingIndicators[key];
                if (!userIds.Contains(key))
                {
                    indicator.Stop();
                    typingIndicators.Remove(key);
                    indicator.Dispose();
                    ;
                }
            }

            foreach (var typingUserId in userIds)
            {
                //Stop the old timer
                if (typingIndicators.TryGetValue(typingUserId, out var typingTimer))
                {
                    typingTimer.Stop();
                }

                //Create and start new timer
                var newTimer = new Timer(chat.Config.TypingTimeout);
                newTimer.Elapsed += (_, _) =>
                {
                    typingIndicators.Remove(typingUserId);
                    OnUsersTyping?.Invoke(typingIndicators.Keys.ToList());
                };
                typingIndicators[typingUserId] = newTimer;
                newTimer.Start();
            }

            OnUsersTyping?.Invoke(userIds);
        }

        public async Task ForwardMessage(Message message)
        {
            await chat.ForwardMessage(message, this);
        }

        public async Task EmitUserMention(string userId, string timeToken, string text)
        {
            throw new NotImplementedException();
        }

        public async Task StartTyping()
        {
            await chat.EmitEvent(PubnubChatEventType.Typing, Id, $"{{\"value\":true}}");
        }

        public async Task StopTyping()
        {
            await chat.EmitEvent(PubnubChatEventType.Typing, Id, $"{{\"value\":false}}");
        }

        public virtual async Task PinMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public virtual async Task UnpinMessage()
        {
            throw new NotImplementedException();
        }

        //TODO: currently same result whether error or no pinned message present
        /// <summary>
        /// Tries to get the <c>Message</c> pinned to this <c>Channel</c>.
        /// </summary>
        /// <param name="pinnedMessage">The pinned Message object, null if there wasn't one.</param>
        /// <returns>True of a pinned Message was found, false otherwise.</returns>
        /// <seealso cref="GetPinnedMessageAsync"/>
        public bool TryGetPinnedMessage(out Message pinnedMessage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously tries to get the <c>Message</c> pinned to this <c>Channel</c>.
        /// </summary>
        /// <returns>The pinned Message object if there was one, null otherwise.</returns>
        public async Task<Message?> GetPinnedMessageAsync()
        {
            return await Task.Run(() =>
            {
                var result = TryGetPinnedMessage(out var pinnedMessage);
                return result ? pinnedMessage : null;
            });
        }

        /// <summary>
        /// Creates a new MessageDraft.
        /// </summary>
        /// <param name="userSuggestionSource">Source of the user suggestions</param>
        /// <param name="isTypingIndicatorTriggered">Typing indicator trigger status.</param>
        /// <param name="userLimit">User limit.</param>
        /// <param name="channelLimit">Channel limit.</param>
        /// <param name="shouldSearchForSuggestions">Whether the MessageDraft should search for suggestions whenever the text is changed.</param>
        /// <returns></returns>
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
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while disconnecting from the channel.</exception>
        /// <seealso cref="Connect"/>
        /// <seealso cref="Join"/>
        public void Disconnect()
        {
            if (subscription == null)
            {
                return;
            }
            subscription.Unsubscribe<object>();
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
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while leaving the channel.</exception>
        /// <seealso cref="Join"/>
        /// <seealso cref="Connect"/>
        /// <seealso cref="Disconnect"/>
        public async void Leave()
        {
            Disconnect();
            var currentUserId = chat.PubnubInstance.GetCurrentUserId();
            var remove = await chat.PubnubInstance.RemoveMemberships().Uuid(currentUserId).Channels(new List<string>() { Id })
                .ExecuteAsync();
            if (remove.Status.Error)
            {
                chat.Logger.Error($"Error when trying to leave channel \"{Id}\": {remove.Status.ErrorData.Information}");
                return;
            }
            
            //TODO: wrappers rethink
            chat.membershipWrappers.Remove(currentUserId + Id);
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
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while connecting to the channel.</exception>
        /// <seealso cref="OnMessageReceived"/>
        /// <seealso cref="Disconnect"/>
        /// <seealso cref="Join"/>
        public void Connect()
        {
            if (subscription != null)
            {
                return;
            }
            subscription = chat.PubnubInstance.Channel(Id).Subscription(SubscriptionOptions.ReceivePresenceEvents);
            subscription.AddListener(chat.ListenerFactory.ProduceListener(messageCallback:
                delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseMessageResult(chat, m, out var message))
                    {
                        chat.RegisterMessage(message);
                        OnMessageReceived?.Invoke(message);
                    }
                }));
            subscription.Subscribe<object>();
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
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while joining the channel.</exception>
        /// <seealso cref="OnMessageReceived"/>
        /// <seealso cref="Connect"/>
        /// <seealso cref="Disconnect"/>
        public async void Join(ChatMembershipData? membershipData = null)
        {
            membershipData ??= new ChatMembershipData();
            var currentUserId = chat.PubnubInstance.GetCurrentUserId();
            var response = await chat.PubnubInstance.SetMemberships().Uuid(currentUserId)
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
                }).ExecuteAsync();
            if (response.Status.Error)
            {
                chat.Logger.Error($"Error when trying to Join() to channel \"{Id}\": {response.Status.ErrorData.Information}");
                return;
            }
            //TODO: wrappers rethink
            if (chat.membershipWrappers.TryGetValue(currentUserId + Id, out var existingHostMembership))
            {
                existingHostMembership.UpdateLocalData(membershipData);
            }
            else
            {
                var joinMembership = new Membership(chat, currentUserId, Id, membershipData);
                await joinMembership.SetLastReadMessageTimeToken(ChatUtils.TimeTokenNow());
                chat.membershipWrappers.Add(joinMembership.Id, joinMembership);
            }
            
            Connect();
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
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.SetRestrictions("user1", true, false, "Spamming");
        /// </code>
        /// </example>
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while setting the restrictions.</exception>
        /// <seealso cref="GetUserRestrictions"/>
        public async Task SetRestrictions(string userId, bool banUser, bool muteUser, string reason)
        {
            throw new NotImplementedException();
        }

        public async Task SetRestrictions(string userId, Restriction restriction)
        {
            await SetRestrictions(userId, restriction.Ban, restriction.Mute, restriction.Reason);
        }

        /// <summary>
        /// Sends the text message.
        /// <para>
        /// Sends the text message to the channel.
        /// The message is sent in the form of a text.
        /// </para>
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.SendText("Hello, World!");
        /// </code>
        /// </example>
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while sending the message.</exception>
        /// <seealso cref="OnMessageReceived"/>
        public virtual async Task SendText(string message)
        {
            await SendText(message, new SendTextParams());
        }

        public virtual async Task SendText(string message, SendTextParams sendTextParams)
        {
            //TODO: maybe move this to a method in config?
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
                return await chat.PubnubInstance.Publish()
                    .Channel(Id)
                    .ShouldStore(sendTextParams.StoreInHistory)
                    .UsePOST(sendTextParams.SendByPost)
                    .Message(chat.PubnubInstance.JsonPluggableLibrary.SerializeToJsonString(messageDict))
                    .Meta(meta)
                    .ExecuteAsync();
            }, response =>
            {
                if (response is PNResult<PNPublishResult> result && result.Status.Error)
                {
                    chat.Logger.Error($"Error occured when trying to SendText(): {result.Status.ErrorData.Information}");
                }
                completionSource.SetResult(true);
            }, exception =>
            {
                chat.Logger.Error($"Error occured when trying to SendText(): {exception.Message}");
                completionSource.SetResult(true);
            });

            await completionSource.Task;
        }

        /// <summary>
        /// Updates the channel.
        /// <para>
        /// Updates the channel with the new data.
        /// The data includes the name, description, custom data, and type of the channel.
        /// </para>
        /// </summary>
        /// <param name="updatedData">The updated data of the channel.</param>
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.UpdateChannel(new ChatChannelData {
        ///  Name = "newName",
        ///  Description = "newDescription",
        ///  CustomDataJson = "{\"key\": \"value\"}",
        ///  Type = "newType"
        /// });
        /// </code>
        /// </example>
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while updating the channel.</exception>
        /// <seealso cref="OnChannelUpdate"/>
        /// <seealso cref="ChatChannelData"/>
        public async Task Update(ChatChannelData updatedData)
        {
            await chat.UpdateChannel(Id, updatedData);
        }

        /// <summary>
        /// Deletes the channel.
        /// <para>
        /// Deletes the channel and removes all the messages and memberships from the channel.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// var channel = //...
        /// channel.DeleteChannel();
        /// </code>
        /// </example>
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while deleting the channel.</exception>
        public async Task Delete()
        {
            await chat.DeleteChannel(Id);
        }

        /// <summary>
        /// Gets the user restrictions.
        /// <para>
        /// Gets the user restrictions that include the information about the bans and mutes.
        /// </para>
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="limit">The maximum amount of the restrictions received.</param>
        /// <param name="startTimetoken">The start timetoken of the restrictions.</param>
        /// <param name="endTimetoken">The end timetoken of the restrictions.</param>
        /// <returns>The user restrictions in JSON format.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var restrictions = channel.GetUserRestrictions(
        ///     "user1",
        ///     10,
        ///     "16686902600029072"
        ///     "16686902600028961",
        /// );
        /// </code>
        /// </example>
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while getting the user restrictions.</exception>
        /// <seealso cref="SetRestrictions"/>
        public async Task<Restriction> GetUserRestrictions(User user)
        {
            throw new NotImplementedException();
        }

        public async Task<UsersRestrictionsWrapper> GetUsersRestrictions(string sort = "", int limit = 0,
            Page page = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether the user is present in the channel.
        /// <para>
        /// The method checks whether the user is present in the channel.
        /// </para>
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns><c>true</c> if the user is present in the channel; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var isUserPresent = channel.IsUserPresent("user1");
        /// Console.WriteLine($"User present: {isUserPresent}");
        /// </code>
        /// </example>
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while checking the presence of the user.</exception>
        /// <seealso cref="WhoIsPresent"/>
        public async Task<bool> IsUserPresent(string userId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the list of users present in the channel.
        /// <para>
        /// Gets all the users that are present in the channel.
        /// </para>
        /// </summary>
        /// <returns>The list of users present in the channel.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var users = channel.WhoIsPresent();
        /// foreach (var user in users) {
        ///  Console.WriteLine($"User present: {user}");
        /// }
        /// </code>
        /// </example>
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while getting the list of users present in the channel.</exception>
        /// <seealso cref="IsUserPresent"/>
        public async Task<List<string>> WhoIsPresent()
        {
            var result = new List<string>();
            var response = await chat.PubnubInstance.HereNow().Channels(new[] { Id }).IncludeState(true)
                .IncludeUUIDs(true).ExecuteAsync();
            if (response.Status.Error)
            {
                chat.Logger.Error($"Error when trying to perform WhoIsPresent(): {response.Status.ErrorData.Information}");
                return result;
            }

            foreach (var occupant in response.Result.Channels[Id].Occupants)
            {
                result.Add(occupant.Uuid);
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
        /// <param name="limit">The maximum amount of the memberships received.</param>
        /// <param name="startTimeToken">The start timetoken of the memberships.</param>
        /// <param name="endTimeToken">The end timetoken of the memberships.</param>
        /// <returns>The list of the <c>Membership</c> objects.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// var memberships = channel.GetMemberships(10, "16686902600029072", "16686902600028961");
        /// foreach (var membership in memberships) {
        ///   Console.WriteLine($"Membership: {membership.UserId}");
        /// }
        /// </code>
        /// </example>
        /// <exception cref="PubnubCCoreException">Thrown when an error occurs while getting the list of memberships.</exception>
        /// <seealso cref="Membership"/>
        public async Task<MembersResponseWrapper?> GetMemberships(string filter = "", string sort = "", int limit = 0,
            PNPageObject page = null)
        {
            return await chat.GetChannelMemberships(Id, filter, sort, limit, page);
        }

        /// <summary>
        /// Gets the <c>Message</c> object for the given timetoken.
        /// <para>
        /// Gets the <c>Message</c> object for the given timetoken.
        /// The timetoken is used to identify the message.
        /// </para>
        /// </summary>
        /// <param name="timeToken">The timetoken of the message.</param>
        /// <param name="message">The out parameter that contains the <c>Message</c> object.</param>
        /// <returns><c>true</c> if the message is found; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <code>
        /// var channel = //...
        /// if (channel.TryGetMessage("16686902600029072", out var message)) {
        ///  Console.WriteLine($"Message: {message.Text}");
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="Message"/>
        /// <seealso cref="GetMessageAsync"/>
        public bool TryGetMessage(string timeToken, out Message message)
        {
            return chat.TryGetMessage(Id, timeToken, out message);
        }

        /// <summary>
        /// Asynchronously gets the <c>Message</c> object for the given timetoken sent from this <c>Channel</c>.
        /// </summary>
        /// <param name="timeToken">TimeToken of the searched-for message.</param>
        /// <returns>Message object if one was found, null otherwise.</returns>
        public async Task<Message?> GetMessageAsync(string timeToken)
        {
            return await chat.GetMessageAsync(Id, timeToken);
        }

        public async Task<List<Message>> GetMessageHistory(string startTimeToken, string endTimeToken,
            int count)
        {
            return await chat.GetChannelMessageHistory(Id, startTimeToken, endTimeToken, count);
        }
        
        public async Task<Membership?> Invite(User user)
        {
            return await chat.InviteToChannel(Id, user.Id);
        }
        
        public async Task<List<Membership>> InviteMultiple(List<User> users)
        {
            return await chat.InviteMultipleToChannel(Id, users);
        }
    }
}