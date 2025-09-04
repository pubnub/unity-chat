using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Entities.Events;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    //TODO: make IDisposable?
    /// <summary>
    /// Main class for the chat.
    /// <para>
    /// Contains all the methods to interact with the chat.
    /// It should be treated as a root of the chat system.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The class is responsible for creating and managing channels, users, and messages.
    /// </remarks>
    public class Chat
    {
        internal const string INTERNAL_MODERATION_PREFIX = "PUBNUB_INTERNAL_MODERATION";
        internal const string INTERNAL_ADMIN_CHANNEL = "PUBNUB_INTERNAL_ADMIN_CHANNEL";
        internal const string MESSAGE_THREAD_ID_PREFIX = "PUBNUB_INTERNAL_THREAD";
        
        public Pubnub PubnubInstance { get; }
        public PubnubLogModule Logger => PubnubInstance.PNConfig.Logger;
        
        internal ChatListenerFactory ListenerFactory { get; }

        public event Action<ChatEvent> OnAnyEvent;

        public ChatAccessManager ChatAccessManager { get; }
        public PubnubChatConfig Config { get; }
        internal ExponentialRateLimiter RateLimiter { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chat"/> class.
        /// <para>
        /// Creates a new chat instance.
        /// </para>
        /// </summary>
        /// <param name="chatConfig">Config with Chat specific parameters</param>
        /// <param name="pubnubConfig">Config with PubNub keys and values</param>
        /// <param name="listenerFactory">Optional injectable listener factory, used in Unity to allow for dispatching Chat callbacks on main thread.</param>
        /// <remarks>
        /// The constructor initializes the Chat object with a new Pubnub instance.
        /// </remarks>
        public static async Task<ChatOperationResult<Chat>> CreateInstance(PubnubChatConfig chatConfig, PNConfiguration pubnubConfig, ChatListenerFactory? listenerFactory = null)
        {
            var chat = new Chat(chatConfig, pubnubConfig, listenerFactory);
            var result = new ChatOperationResult<Chat>(){Result = chat};
            var getUser = await chat.GetCurrentUser();
            if (getUser.Error)
            {
                result.RegisterOperation(await chat.CreateUser(chat.PubnubInstance.GetCurrentUserId()));
            }
            return result;
        }
        
        internal Chat(PubnubChatConfig chatConfig, PNConfiguration pubnubConfig, ChatListenerFactory? listenerFactory = null)
        {
            PubnubInstance = new Pubnub(pubnubConfig);
            ListenerFactory = listenerFactory ?? new DotNetListenerFactory();
            Config = chatConfig;
            ChatAccessManager = new ChatAccessManager(this);
            RateLimiter = new ExponentialRateLimiter(chatConfig.RateLimitFactor);
        }
        
        internal Chat(PubnubChatConfig chatConfig, Pubnub pubnub, ChatListenerFactory? listenerFactory = null)
        {
            Config = chatConfig;
            PubnubInstance = pubnub;
            ListenerFactory = listenerFactory ?? new DotNetListenerFactory();
            ChatAccessManager = new ChatAccessManager(this);
            RateLimiter = new ExponentialRateLimiter(chatConfig.RateLimitFactor);
        }
        
        #region Channels

        public async Task AddListenerToChannelsUpdate(List<string> channelIds, Action<Channel> listener)
        {
            foreach (var channelId in channelIds)
            {
                var getResult = await GetChannel(channelId);
                if (!getResult.Error)
                {
                    getResult.Result.OnChannelUpdate += listener;
                }
            }
        }
        
        /// <summary>
        /// Creates a new public conversation.
        /// <para>
        /// Creates a new public conversation with the provided channel ID.
        /// Conversation allows users to interact with each other.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <returns>The created channel.</returns>
        /// <remarks>
        /// The method creates a chat channel with the provided channel ID.
        /// </remarks>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var channel = chat.CreatePublicConversation("channel_id");
        /// </code>
        /// </example>
        /// <seealso cref="Channel"/>
        public async Task<ChatOperationResult<Channel>> CreatePublicConversation(string channelId = "")
        {
            if (string.IsNullOrEmpty(channelId))
            {
                channelId = Guid.NewGuid().ToString();
            }

            return await CreatePublicConversation(channelId, new ChatChannelData());
        }

        /// <summary>
        /// Creates a new public conversation.
        /// <para>
        /// Creates a new public conversation with the provided channel ID.
        /// Conversation allows users to interact with each other.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="additionalData">The additional data for the channel.</param>
        /// <returns>The created channel.</returns>
        /// <remarks>
        /// The method creates a chat channel with the provided channel ID.
        /// </remarks>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var channel = chat.CreatePublicConversation("channel_id");
        /// </code>
        /// </example>
        /// <seealso cref="Channel"/>
        /// <seealso cref="ChatChannelData"/>
        public async Task<ChatOperationResult<Channel>> CreatePublicConversation(string channelId, ChatChannelData additionalData)
        {
            var result = new ChatOperationResult<Channel>();
            var existingChannel = await GetChannel(channelId);
            if (!result.RegisterOperation(existingChannel))
            {
                Logger.Debug("Trying to create a channel with ID that already exists! Returning existing one.");
                result.Result = existingChannel.Result;
                return result;
            }

            additionalData.Type = "public";
            var updated = await Channel.UpdateChannelData(this, channelId, additionalData);
            if (result.RegisterOperation(updated))
            {
                return result;
            }
            var channel = new Channel(this, channelId, additionalData);
            result.Result = channel;
            return result;
        }

        private async Task<ChatOperationResult<CreatedChannelWrapper>> CreateConversation(
            string type, 
            List<User> users, 
            string channelId = "", 
            ChatChannelData? channelData = null, 
            ChatMembershipData? membershipData = null)
        {
            var result = new ChatOperationResult<CreatedChannelWrapper>(){Result = new CreatedChannelWrapper()};
            
            if (string.IsNullOrEmpty(channelId))
            {
                channelId = Guid.NewGuid().ToString();
            }
            
            var existingChannel = await GetChannel(channelId);
            if (!result.RegisterOperation(existingChannel))
            {
                Logger.Debug("Trying to create a channel with ID that already exists! Returning existing one.");
                result.Result.CreatedChannel = existingChannel.Result;
                return result;
            }
            
            channelData ??= new ChatChannelData();
            channelData.Type = type;
            var updated = await Channel.UpdateChannelData(this, channelId, channelData);
            if (result.RegisterOperation(updated))
            {
                return result;
            }
            
            membershipData ??= new ChatMembershipData();
            var currentUserId = PubnubInstance.GetCurrentUserId();
            var setMembershipResult = await PubnubInstance.SetMemberships()
                .Uuid(currentUserId)
                .Include(
                    new[]
                    {
                        PNMembershipField.CHANNEL_CUSTOM,
                        PNMembershipField.CUSTOM,
                        PNMembershipField.CHANNEL,
                        PNMembershipField.STATUS,
                        PNMembershipField.TYPE,
                    })
                .Channels(new List<PNMembership>() { new ()
                {
                    Channel = channelId,
                    Custom = membershipData.CustomData,
                    Status = membershipData.Status,
                    Type = membershipData.Type
                }})
                .ExecuteAsync();

            if (result.RegisterOperation(setMembershipResult))
            {
                return result;
            }
            
            var hostMembership = new Membership(this, currentUserId, channelId, membershipData);
            result.Result.HostMembership = hostMembership;
            
            var channel = new Channel(this, channelId, channelData);
            result.Result.CreatedChannel = channel;

            if (type == "direct")
            {
                var inviteMembership = await InviteToChannel(channelId, users[0].Id);
                if (result.RegisterOperation(inviteMembership))
                {
                    return result;
                }
                result.Result.InviteesMemberships = new List<Membership>() { inviteMembership.Result };
            }else if (type == "group")
            {
                var inviteMembership = await InviteMultipleToChannel(channelId, users);
                if (result.RegisterOperation(inviteMembership))
                {
                    return result;
                }
                result.Result.InviteesMemberships = new List<Membership>(inviteMembership.Result);
            }
            return result;
        }

        public async Task<ChatOperationResult<CreatedChannelWrapper>> CreateDirectConversation(User user, string channelId = "",
            ChatChannelData? channelData = null, ChatMembershipData? membershipData = null)
        {
            return await CreateConversation("direct", new List<User>() { user }, channelId, channelData,
                membershipData);
        }

        public async Task<ChatOperationResult<CreatedChannelWrapper>> CreateGroupConversation(List<User> users, string channelId = "",
            ChatChannelData? channelData = null, ChatMembershipData? membershipData = null)
        {
            return await CreateConversation("group", users, channelId, channelData,
                membershipData);
        }

        public async Task<ChatOperationResult<Membership>> InviteToChannel(string channelId, string userId)
        {
            var result = new ChatOperationResult<Membership>();
            //Check if already a member first
            var members = await GetChannelMemberships(channelId, filter:$"uuid.id == \"{userId}\"");
            if (!result.RegisterOperation(members))
            {
                //Already a member, just return current membership
                result.Result = members.Result.Memberships[0];
                return result;
            }
            
            var channel = await GetChannel(channelId);
            if (result.RegisterOperation(channel))
            {
                return result;
            }

            var setMemberships = await PubnubInstance.SetMemberships().Uuid(userId).Include(new[]
            {
                PNMembershipField.CUSTOM,
                PNMembershipField.TYPE,
                PNMembershipField.CHANNEL,
                PNMembershipField.CHANNEL_CUSTOM,
                PNMembershipField.STATUS
            }).Channels(new List<PNMembership>()
            {
                new()
                {
                    Channel = channelId,
                    //TODO: these too here?
                    //TODO: again, should ChatMembershipData from Create(...)Channel also be passed here?
                    /*Custom = ,
                    Status = ,
                    Type = */
                }
            }).ExecuteAsync();

            if (result.RegisterOperation(setMemberships))
            {
                return result;
            }
            
            var newMataData = setMemberships.Result.Memberships?.FirstOrDefault(x => x.ChannelMetadata.Channel == channelId)?
                .ChannelMetadata;
            if (newMataData != null)
            {
                channel.Result.UpdateLocalData(newMataData);
            }

            var inviteEventPayload = $"{{\"channelType\": \"{channel.Result.Type}\", \"channelId\": {channelId}}}";
            await EmitEvent(PubnubChatEventType.Invite, userId, inviteEventPayload);
            
            var newMembership = new Membership(this, userId, channelId, new ChatMembershipData());
            await newMembership.SetLastReadMessageTimeToken(ChatUtils.TimeTokenNow());

            result.Result = newMembership;
            return result;
        }

        public async Task<ChatOperationResult<List<Membership>>> InviteMultipleToChannel(string channelId, List<User> users)
        {
            var result = new ChatOperationResult<List<Membership>>() { Result = new List<Membership>() };
            var channel = await GetChannel(channelId);
            if (result.RegisterOperation(channel))
            {
                return result;
            }
            var inviteResponse = await PubnubInstance.SetChannelMembers().Channel(channelId)
                .Include(
                    new[] { 
                        PNChannelMemberField.UUID, 
                        PNChannelMemberField.CUSTOM, 
                        PNChannelMemberField.UUID_CUSTOM,
                        PNChannelMemberField.TYPE,
                        PNChannelMemberField.STATUS,
                        PNChannelMemberField.UUID_TYPE,
                        PNChannelMemberField.UUID_STATUS
                    })
                //TODO: again, should ChatMembershipData from Create(...)Channel  also be passed here?
                .Uuids(users.Select(x => new PNChannelMember() { Custom = x.CustomData, Uuid = x.Id }).ToList())
                .ExecuteAsync();
            
            if (result.RegisterOperation(inviteResponse))
            {
                return result;
            }
            
            foreach (var channelMember in inviteResponse.Result.ChannelMembers)
            {
                var userId = channelMember.UuidMetadata.Uuid;
                var newMembership = new Membership(this, userId, channelId, channelMember);
                await newMembership.SetLastReadMessageTimeToken(ChatUtils.TimeTokenNow());
                result.Result.Add(newMembership);
                
                var inviteEventPayload = $"{{\"channelType\": \"{channel.Result.Type}\", \"channelId\": {channelId}}}";
                await EmitEvent(PubnubChatEventType.Invite, userId, inviteEventPayload);
            }

            await channel.Result.Refresh();

            return result;
        }

        /// <summary>
        /// Performs an async retrieval of a Channel object with a given ID.
        /// </summary>
        /// <param name="channelId">ID of the channel.</param>
        /// <returns>Channel object if it exists, null otherwise.</returns>
        public async Task<ChatOperationResult<Channel>> GetChannel(string channelId)
        {
            var result = new ChatOperationResult<Channel>();
            var getResult = await Channel.GetChannelData(this, channelId);
            if (result.RegisterOperation(getResult))
            {
                return result;
            }
            var channel = new Channel(this, channelId, getResult.Result);
            result.Result = channel;
            return result;
        }

        public async Task<ChannelsResponseWrapper> GetChannels(string filter = "", string sort = "", int limit = 0,
            PNPageObject page = null)
        {
            var operation = PubnubInstance.GetAllChannelMetadata().IncludeCustom(true).IncludeCount(true).IncludeStatus(true).IncludeType(true);
            if (!string.IsNullOrEmpty(filter))
            {
                operation = operation.Filter(filter);
            }
            if (!string.IsNullOrEmpty(sort))
            {
                operation = operation.Sort(new List<string>(){sort});
            }
            if (limit > 0)
            {
                operation = operation.Limit(limit);
            }
            if (page != null)
            {
                operation = operation.Page(page);
            }
            var response = await operation.ExecuteAsync();
            
            if (response.Status.Error)
            {
                Logger.Error($"Error when trying to GetChannels(): {response.Status.ErrorData.Information}");
                return default;
            }
            var wrapper = new ChannelsResponseWrapper()
            {
                Channels = new List<Channel>(),
                Total = response.Result.TotalCount,
                Page = response.Result.Page
            };
            foreach (var resultMetadata in response.Result.Channels)
            {
                var channel = new Channel(this, resultMetadata.Channel, resultMetadata);
                wrapper.Channels.Add(channel);
            }
            return wrapper;
        }

        /// <summary>
        /// Updates the channel with the provided channel ID.
        /// <para>
        /// Updates the channel with the provided channel ID with the provided data.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="updatedData">The updated data for the channel.</param>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// chat.UpdateChannel("channel_id", new ChatChannelData {
        ///    ChannelName = "new_name"
        ///    // ...
        ///  });
        /// </code>
        /// </example>
        /// <seealso cref="ChatChannelData"/>
        public async Task<ChatOperationResult> UpdateChannel(string channelId, ChatChannelData updatedData)
        {
            var result = new ChatOperationResult();
            result.RegisterOperation(await Channel.UpdateChannelData(this, channelId, updatedData));
            return result;
        }

        /// <summary>
        /// Deletes the channel with the provided channel ID.
        /// <para>
        /// The channel is deleted with all the messages and users.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// chat.DeleteChannel("channel_id");
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> DeleteChannel(string channelId)
        {
            var result = new ChatOperationResult();
            result.RegisterOperation(await PubnubInstance.RemoveChannelMetadata().Channel(channelId).ExecuteAsync());
            return result;
        }

        #endregion

        #region Users

        public async Task<ChatOperationResult<UserMentionsWrapper>> GetCurrentUserMentions(string startTimeToken, string endTimeToken,
            int count)
        {
            var result = new ChatOperationResult<UserMentionsWrapper>();
            var id = PubnubInstance.GetCurrentUserId();
            var getEventHistory = await GetEventsHistory(id, startTimeToken, endTimeToken, count);
            if (result.RegisterOperation(getEventHistory))
            {
                return result;
            }
            var wrapper = new UserMentionsWrapper()
            {
                IsMore = getEventHistory.Result.IsMore,
                Mentions = new List<UserMentionData>()
            };
            var mentionEvents = getEventHistory.Result.Events.Where(x => x.Type == PubnubChatEventType.Mention);
            foreach (var mentionEvent in mentionEvents)
            {
                var payloadDict =
                    PubnubInstance.JsonPluggableLibrary.DeserializeToDictionaryOfObject(mentionEvent.Payload);
                if (!payloadDict.TryGetValue("text", out var mentionText) 
                    || !payloadDict.TryGetValue("messageTimetoken", out var messageTimeToken) 
                    || !payloadDict.TryGetValue("channel", out var mentionChannel))
                {
                    continue;
                }
                var getMessage = await GetMessage(mentionChannel.ToString(), messageTimeToken.ToString());
                if (getMessage.Error)
                {
                    Logger.Warn($"Could not find message with ID/Timetoken from mention event. Event payload: {mentionEvent.Payload}");
                    continue;
                }

                var mention = new UserMentionData()
                {
                    ChannelId = mentionChannel.ToString(),
                    Event = mentionEvent,
                    Message = getMessage.Result,
                    UserId = mentionEvent.UserId
                };
                if (payloadDict.TryGetValue("parentChannel", out var parentChannelId))
                {
                    mention.ParentChannelId = parentChannelId.ToString();
                }
                wrapper.Mentions.Add(mention);
            }
            result.Result = wrapper;
            return result;
        }

        /// <summary>
        /// Asynchronously tries to retrieve the current User object for this chat.
        /// </summary>
        /// <returns>User object if there is a current user, null otherwise.</returns>
        public async Task<ChatOperationResult<User>> GetCurrentUser()
        {
            var result = new ChatOperationResult<User>();
            var userId = PubnubInstance.GetCurrentUserId();
            var getUser = await GetUser(userId);
            if (result.RegisterOperation(getUser))
            {
                return result;
            }
            result.Result = getUser.Result;
            return result;
        }

        /// <summary>
        /// Sets the restrictions for the user with the provided user ID.
        /// <para>
        /// Sets the restrictions for the user with the provided user ID in the provided channel.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="banUser">The ban user flag.</param>
        /// <param name="muteUser">The mute user flag.</param>
        /// <param name="reason">The reason for the restrictions.</param>
        /// <example>
        /// <code>
        /// await chat.SetRestriction("user_id", "channel_id", true, true, "Spamming");
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> SetRestriction(string userId, string channelId, bool banUser, bool muteUser, string reason)
        {
            var result = new ChatOperationResult();
            var restrictionsChannelId = $"{INTERNAL_MODERATION_PREFIX}_{channelId}";
            var getResult = await Channel.GetChannelData(this, restrictionsChannelId);
            if (result.RegisterOperation(getResult))
            {
                if (result.RegisterOperation(await Channel.UpdateChannelData(this, restrictionsChannelId,
                        new ChatChannelData())))
                {
                    return result;
                }
            }
            var moderationEventsChannelId = INTERNAL_MODERATION_PREFIX + userId;
            //Lift restrictions
            if (!banUser && !muteUser)
            {
                if (result.RegisterOperation(await PubnubInstance.RemoveChannelMembers().Channel(restrictionsChannelId)
                        .Uuids(new List<string>() { userId }).ExecuteAsync()))
                {
                    return result;
                }
                result.RegisterOperation(await EmitEvent(PubnubChatEventType.Moderation, moderationEventsChannelId,
                    $"{{\"channelId\": \"{channelId}\", \"restriction\": \"lifted\", \"reason\": \"{reason}\"}}"));
                return result;
            }
            //Ban or mute
            if (result.RegisterOperation(await PubnubInstance.SetChannelMembers().Channel(restrictionsChannelId).Uuids(new List<PNChannelMember>()
                {
                    new PNChannelMember()
                    {
                        Uuid = userId,
                        Custom = new Dictionary<string, object>()
                        {
                            { "ban", banUser },
                            { "mute", muteUser },
                            { "reason", reason }
                        }
                    }
                }).Include(new PNChannelMemberField[]
                {
                    PNChannelMemberField.UUID,
                    PNChannelMemberField.CUSTOM
                }).ExecuteAsync()))
            {
                return result;
            }
            result.RegisterOperation(await EmitEvent(PubnubChatEventType.Moderation, moderationEventsChannelId,
                $"{{\"channelId\": \"{channelId}\", \"restriction\": \"{(banUser ? "banned" : "muted")}\", \"reason\": \"{reason}\"}}"));
            return result;
        }

        /// <summary>
        /// Sets the restrictions for the user with the provided user ID.
        /// <para>
        /// Sets the restrictions for the user with the provided user ID in the provided channel.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="restriction">The Restriction object to be applied.</param>
        /// <example>
        /// <code>
        /// await chat.SetRestriction("user_id", "channel_id", new Restriction(){Ban = true, Mute = true, Reason = "Spamming"});
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> SetRestriction(string userId, string channelId, Restriction restriction)
        {
            return await SetRestriction(userId, channelId, restriction.Ban, restriction.Mute, restriction.Reason);
        }

        public async void AddListenerToUsersUpdate(List<string> userIds, Action<User> listener)
        {
            foreach (var userId in userIds)
            {
                var getUser = await GetUser(userId);
                if (!getUser.Error)
                {
                    getUser.Result.OnUserUpdated += listener;
                }
            }
        }
        
        /// <summary>
        /// Creates a new user with the provided user ID.
        /// <para>
        /// Creates a new user with the empty data and the provided user ID.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The created user.</returns>
        /// <remarks>
        /// The data for user is empty.
        /// </remarks>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var user = chat.CreateUser("user_id");
        /// </code>
        /// </example>
        /// <seealso cref="User"/>
        public async Task<ChatOperationResult<User>> CreateUser(string userId)
        {
            return await CreateUser(userId, new ChatUserData());
        }

        /// <summary>
        /// Creates a new user with the provided user ID.
        /// <para>
        /// Creates a new user with the provided data and the provided user ID.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="additionalData">The additional data for the user.</param>
        /// <returns>The created user.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var user = chat.CreateUser("user_id");
        /// </code>
        /// </example>
        /// <seealso cref="User"/>
        public async Task<ChatOperationResult<User>> CreateUser(string userId, ChatUserData additionalData)
        {
            var result = new ChatOperationResult<User>();
            var existingUser = await GetUser(userId);
            if (!result.RegisterOperation(existingUser))
            {
                result.Result = existingUser.Result;
                return result;
            }
            
            var update = await User.UpdateUserData(this, userId, additionalData);
            if (result.RegisterOperation(update))
            {
                return result;
            }
            var user = new User(this, userId, additionalData);
            result.Result = user;
            return result;
        }

        /// <summary>
        /// Checks if the user with the provided user ID is present in the provided channel.
        /// <para>
        /// Checks if the user with the provided user ID is present in the provided channel.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="channelId">The channel ID.</param>
        /// <returns>True if the user is present, false otherwise.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// if (chat.IsPresent("user_id", "channel_id")) {
        ///   // User is present 
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="WhoIsPresent"/>
        /// <seealso cref="WherePresent"/>
        public async Task<ChatOperationResult<bool>> IsPresent(string userId, string channelId)
        {
            var result = new ChatOperationResult<bool>();
            var getChannel = await GetChannel(channelId);
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            var isPresent = await getChannel.Result.IsUserPresent(userId);
            if (result.RegisterOperation(isPresent))
            {
                return result;
            }
            result.Result = isPresent.Result;
            return result;
        }

        /// <summary>
        /// Gets the list of users present in the provided channel.
        /// <para>
        /// Gets all the users as a list of the strings present in the provided channel.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <returns>The list of the users present in the channel.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var users = chat.WhoIsPresent("channel_id");
        /// foreach (var user in users) {
        ///   // User is present on the channel
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="WherePresent"/>
        /// <seealso cref="IsPresent"/>
        public async Task<ChatOperationResult<List<string>>> WhoIsPresent(string channelId)
        {
            var result = new ChatOperationResult<List<string>>() { Result = new List<string>() };
            var getChannel = await GetChannel(channelId);
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            var whoIs = await getChannel.Result.WhoIsPresent();
            if (result.RegisterOperation(whoIs))
            {
                return result;
            }
            result.Result = whoIs.Result;
            return result;
        }

        /// <summary>
        /// Gets the list of channels where the user with the provided user ID is present.
        /// <para>
        /// Gets all the channels as a list of the strings where the user with the provided user ID is present.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The list of the channels where the user is present.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var channels = chat.WherePresent("user_id");
        /// foreach (var channel in channels) {
        ///  // Channel where User is IsPresent
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="WhoIsPresent"/>
        /// <seealso cref="IsPresent"/>
        public async Task<ChatOperationResult<List<string>>> WherePresent(string userId)
        {
            var result = new ChatOperationResult<List<string>>() { Result = new List<string>() };
            var getUser = await GetUser(userId);
            if (result.RegisterOperation(getUser))
            {
                return result;
            }
            var wherePresent = await getUser.Result.WherePresent();
            if (result.RegisterOperation(wherePresent))
            {
                return result;
            }
            result.Result = wherePresent.Result;
            return result;
        }

        /// <summary>
        /// Asynchronously gets the user with the provided user ID.
        /// </summary>
        /// <param name="userId">ID of the User to get.</param>
        /// <returns>User object if one with given ID is found, null otherwise.</returns>
        public async Task<ChatOperationResult<User>> GetUser(string userId)
        {
            var result = new ChatOperationResult<User>();
            var getData = await User.GetUserData(this, userId);
            if (result.RegisterOperation(getData))
            {
                return result;
            }
            var user = new User(this, userId, getData.Result);
            result.Result = user;
            return result;
        }
        
        /// <summary>
        /// Gets the list of users with the provided parameters.
        /// <para>
        /// Gets all the users that matches the provided parameters.
        /// </para>
        /// </summary>
        /// <param name="include">The include parameter.</param>
        /// <param name="limit">The amount of userts to get.</param>
        /// <param name="startTimeToken">The start time token of the users.</param>
        /// <param name="endTimeToken">The end time token of the users.</param>
        /// <returns>The list of the users that matches the provided parameters.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var users = chat.GetUsers(
        ///     "admin",
        ///     10,
        ///     "16686902600029072"
        ///     "16686902600028961",
        /// );
        /// foreach (var user in users) {
        ///  // User found
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="User"/>
        public async Task<UsersResponseWrapper> GetUsers(string filter = "", string sort = "", int limit = 0,
            PNPageObject page = null)
        {
            var operation = PubnubInstance.GetAllUuidMetadata().IncludeCustom(true).IncludeStatus(true).IncludeType(true);
            if (!string.IsNullOrEmpty(filter))
            {
                operation = operation.Filter(filter);
            }
            if (!string.IsNullOrEmpty(sort))
            {
                operation = operation.Sort(new List<string>(){sort});
            }
            if (limit > 0)
            {
                operation = operation.Limit(limit);
            }
            if (page != null)
            {
                operation = operation.Page(page);
            }
            var result = await operation.ExecuteAsync();
            
            if (result.Status.Error)
            {
                Logger.Error($"Error when trying to GetUsers(): {result.Status.ErrorData.Information}");
                return default;
            }

            var response = new UsersResponseWrapper()
            {
                Users = new List<User>(),
                Total = result.Result.TotalCount,
                Page = result.Result.Page
            };
            foreach (var resultMetadata in result.Result.Uuids)
            {
                var user = new User(this, resultMetadata.Uuid, resultMetadata);
                response.Users.Add(user);
            }
            return response;
        }
        
        /// <summary>
        /// Updates the user with the provided user ID.
        /// <para>
        /// Updates the user with the provided user ID with the provided data.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="updatedData">The updated data for the user.</param>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// chat.UpdateUser("user_id", new ChatUserData {
        ///   Username = "new_name"
        ///   // ...
        /// });
        /// </code>
        /// </example>
        /// <seealso cref="ChatUserData"/>
        public async Task UpdateUser(string userId, ChatUserData updatedData)
        {
            await User.UpdateUserData(this, userId, updatedData);
        }

        /// <summary>
        /// Deletes the user with the provided user ID.
        /// <para>
        /// The user is deleted with all the messages and channels.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// chat.DeleteUser("user_id");
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> DeleteUser(string userId)
        {
            var result = new ChatOperationResult();
            result.RegisterOperation(await PubnubInstance.RemoveUuidMetadata().Uuid(userId).ExecuteAsync());
            return result;
        }

        #endregion

        #region Memberships

        /// <summary>
        /// Gets the memberships of the user with the provided user ID.
        /// <para>
        /// Gets all the memberships of the user with the provided user ID.
        /// The memberships are limited by the provided limit and the time tokens.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="limit">The maximum amount of the memberships.</param>
        /// <param name="startTimeToken">The start time token of the memberships.</param>
        /// <param name="endTimeToken">The end time token of the memberships.</param>
        /// <returns>The list of the memberships of the user.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var memberships = chat.GetUserMemberships(
        ///         "user_id",
        ///         10,
        ///         "16686902600029072",
        ///         "16686902600028961"
        /// );
        /// foreach (var membership in memberships) {
        ///  // Membership found
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="Membership"/>
        public async Task<ChatOperationResult<MembersResponseWrapper>> GetUserMemberships(string userId, string filter = "",
            string sort = "",
            int limit = 0, PNPageObject page = null)
        {
            var result = new ChatOperationResult<MembersResponseWrapper>();
            var operation = PubnubInstance.GetMemberships().Include(
                new[]
                {
                    PNMembershipField.CHANNEL_CUSTOM,
                    PNMembershipField.CHANNEL_TYPE,
                    PNMembershipField.CHANNEL_STATUS,
                    PNMembershipField.CUSTOM,
                    PNMembershipField.TYPE,
                    PNMembershipField.STATUS,
                }).Uuid(userId);
            if (!string.IsNullOrEmpty(filter))
            {
                operation = operation.Filter(filter);
            }
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
                operation.Page(page);
            }
            var getMemberships = await operation.ExecuteAsync();
            if (result.RegisterOperation(getMemberships))
            {
                return result;
            }

            var memberships = new List<Membership>();
            foreach (var membershipResult in getMemberships.Result.Memberships)
            {
                memberships.Add(new Membership(this, userId, membershipResult.ChannelMetadata.Channel, new ChatMembershipData()
                {
                    CustomData = membershipResult.Custom,
                    Status = membershipResult.Status,
                    Type = membershipResult.Type
                }));
            }
            result.Result = new MembersResponseWrapper()
            {
                Memberships = memberships,
                Page = getMemberships.Result.Page,
                Total = getMemberships.Result.TotalCount
            };
            return result;
        }

        /// <summary>
        /// Gets the memberships of the channel with the provided channel ID.
        /// <para>
        /// Gets all the memberships of the channel with the provided channel ID.
        /// The memberships are limited by the provided limit and the time tokens.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="limit">The maximum amount of the memberships.</param>
        /// <param name="startTimeToken">The start time token of the memberships.</param>
        /// <param name="endTimeToken">The end time token of the memberships.</param>
        /// <returns>The list of the memberships of the channel.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var memberships = chat.GetChannelMemberships(
        ///         "user_id",
        ///         10,
        ///         "16686902600029072",
        ///         "16686902600028961"
        /// );
        /// foreach (var membership in memberships) {
        ///  // Membership found
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="Membership"/>
        public async Task<ChatOperationResult<MembersResponseWrapper>> GetChannelMemberships(string channelId, string filter = "",
            string sort = "",
            int limit = 0, PNPageObject page = null)
        {
            var result = new ChatOperationResult<MembersResponseWrapper>();
            var operation = PubnubInstance.GetChannelMembers().Include(
                    new[]
                    {
                        PNChannelMemberField.UUID_CUSTOM,
                        PNChannelMemberField.UUID_TYPE,
                        PNChannelMemberField.UUID_STATUS,
                        PNChannelMemberField.CUSTOM,
                        PNChannelMemberField.TYPE,
                        PNChannelMemberField.STATUS,
                    }).Channel(channelId);
            if (!string.IsNullOrEmpty(filter))
            {
                operation = operation.Filter(filter);
            }
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

            var getResult = await operation.ExecuteAsync();
            if (result.RegisterOperation(getResult))
            {
                return result;
            }

            var memberships = new List<Membership>();
            foreach (var channelMemberResult in getResult.Result.ChannelMembers)
            {
                memberships.Add(new Membership(this, channelMemberResult.UuidMetadata.Uuid, channelId, new ChatMembershipData()
                {
                    CustomData = channelMemberResult.Custom,
                    Status = channelMemberResult.Status,
                    Type = channelMemberResult.Type
                }));
            }
            result.Result = new MembersResponseWrapper()
            {
                Memberships = memberships,
                Page = getResult.Result.Page,
                Total = getResult.Result.TotalCount
            };
            return result;
        }

        #endregion

        #region Messages
        
        public async Task<ChatOperationResult<EventsHistoryWrapper>> GetMessageReportsHistory(string channelId, string startTimeToken,
            string endTimeToken, int count)
        {
            return await GetEventsHistory($"PUBNUB_INTERNAL_MODERATION_{channelId}", startTimeToken, endTimeToken,
                count);
        }

        /// <summary>
        /// Asynchronously gets the <c>Message</c> object for the given timetoken.
        /// </summary>
        /// <param name="channelId">ID of the channel on which the message was sent.</param>
        /// <param name="messageTimeToken">TimeToken of the searched-for message.</param>
        /// <returns>Message object if one was found, null otherwise.</returns>
        public async Task<ChatOperationResult<Message>> GetMessage(string channelId, string messageTimeToken)
        {
            var result = new ChatOperationResult<Message>();
            var startTimeToken = (long.Parse(messageTimeToken) + 1).ToString();
            var getHistory = await GetChannelMessageHistory(channelId, startTimeToken, messageTimeToken, 1);
            if (result.RegisterOperation(getHistory))
            {
                return result;
            }
            if (!getHistory.Result.Any())
            {
                result.Error = true;
                result.Exception = new PNException($"Didn't find any message with timetoken {messageTimeToken} on channel {channelId}");
                return result;
            }
            result.Result = getHistory.Result[0];
            return result;
        }

        public async Task<ChatOperationResult<MarkMessagesAsReadWrapper>> MarkAllMessagesAsRead(string filter = "", string sort = "",
            int limit = 0,
            PNPageObject page = null)
        {
            var result = new ChatOperationResult<MarkMessagesAsReadWrapper>();
            if (limit < 0 || limit > 100)
            {
                result.Error = true;
                result.Exception = new PNException("For marking messages as read limit has to be between 0 and 100");
                return result;
            }
            var currentUserId = PubnubInstance.GetCurrentUserId();
            var getCurrentUser = await GetCurrentUser();
            if (result.RegisterOperation(getCurrentUser))
            {
                return result;
            }
            var getCurrentMemberships = await getCurrentUser.Result.GetMemberships(filter, sort, limit, page);
            if (result.RegisterOperation(getCurrentMemberships))
            {
                return result;
            }
            if (getCurrentMemberships.Result.Memberships == null || !getCurrentMemberships.Result.Memberships.Any())
            {
                result.Result = new MarkMessagesAsReadWrapper()
                {
                    Memberships = new List<Membership>()
                };
                return result;
            }
            var timeToken = ChatUtils.TimeTokenNow();
            var memberships = getCurrentMemberships.Result.Memberships;
            foreach (var membership in memberships)
            {
                membership.MembershipData.CustomData["lastReadMessageTimetoken"] = timeToken;
            }
            if (result.RegisterOperation(await Membership.UpdateMembershipsData(this, currentUserId, memberships)))
            {
                return result;
            }
            foreach (var membership in memberships)
            {
                await EmitEvent(PubnubChatEventType.Receipt, membership.ChannelId,
                    $"{{\"messageTimetoken\": \"{timeToken}\"}}");
            }
            result.Result = new MarkMessagesAsReadWrapper()
            {
                Memberships = memberships,
                Page = getCurrentMemberships.Result.Page,
                Status = getCurrentMemberships.Result.Status,
                Total = getCurrentMemberships.Result.Total
            };
            return result;
        }
        
        public async Task<ChatOperationResult<List<UnreadMessageWrapper>>> GetUnreadMessagesCounts(string filter = "", string sort = "",
            int limit = 0,
            PNPageObject page = null)
        {
            var result = new ChatOperationResult<List<UnreadMessageWrapper>>();
            if (limit < 0 || limit > 100)
            {
                result.Error = true;
                result.Exception = new PNException("For getting message counts limit has to be between 0 and 100");
                return result;
            }
            var getCurrentUser = await GetCurrentUser();
            if (result.RegisterOperation(getCurrentUser))
            {
                return result;
            }
            var getCurrentMemberships = await getCurrentUser.Result.GetMemberships(filter, sort, limit, page);
            if (result.RegisterOperation(getCurrentMemberships))
            {
                return result;
            }
            if (getCurrentMemberships.Result.Memberships == null || !getCurrentMemberships.Result.Memberships.Any())
            {
                result.Result = new List<UnreadMessageWrapper>();
                return result;
            }
            var memberships = getCurrentMemberships.Result.Memberships;
            var channelIds = new List<string>();
            var timeTokens = new List<long>();
            foreach (var membership in memberships)
            {
                channelIds.Add(membership.ChannelId);
                var lastRead = string.IsNullOrEmpty(membership.LastReadMessageTimeToken) ? Membership.EMPTY_TIMETOKEN : long.Parse(membership.LastReadMessageTimeToken);
                timeTokens.Add(lastRead);
            }
            //TODO: ISSUE: count also includes events
            var getCounts = await PubnubInstance.MessageCounts().Channels(channelIds.ToArray()).ChannelsTimetoken(timeTokens.ToArray())
                .ExecuteAsync();
            if (result.RegisterOperation(getCounts))
            {
                return result;
            }
            var wrapperList = new List<UnreadMessageWrapper>();
            foreach (var channelMessagesCount in getCounts.Result.Channels)
            {
                wrapperList.Add(new UnreadMessageWrapper()
                {
                    ChannelId = channelMessagesCount.Key,
                    Count = Convert.ToInt32(channelMessagesCount.Value),
                    Membership = memberships.First(x => x.ChannelId == channelMessagesCount.Key)
                });
            }
            result.Result = wrapperList;
            return result;
        }

        public async Task<ChatOperationResult<ThreadChannel>> CreateThreadChannel(string messageTimeToken, string messageChannelId)
        {
            var result = new ChatOperationResult<ThreadChannel>();
            var getMessage = await GetMessage(messageChannelId, messageTimeToken);
            if (result.RegisterOperation(getMessage))
            {
                return result;
            }
            var createThread = getMessage.Result.CreateThread();
            if (result.RegisterOperation(createThread))
            {
                return result;
            }
            result.Result = createThread.Result;
            return result;
        }

        public async Task<ChatOperationResult> RemoveThreadChannel(string messageTimeToken, string messageChannelId)
        {
            var result = new ChatOperationResult();
            var getMessage = await GetMessage(messageChannelId, messageTimeToken);
            if (result.RegisterOperation(getMessage))
            {
                return result;
            }
            result.RegisterOperation(await getMessage.Result.RemoveThread());
            return result;
        }

        /// <summary>
        /// Asynchronously tries to retrieve a ThreadChannel object from a Message object if there is one.
        /// </summary>
        /// <param name="message">Message on which the ThreadChannel is supposed to be.</param>
        /// <returns>The ThreadChannel object if one was found, null otherwise.</returns>
        public async Task<ChatOperationResult<ThreadChannel>> GetThreadChannel(Message message)
        {
            var result = new ChatOperationResult<ThreadChannel>();
            var getChannel = await GetChannel(message.GetThreadId());
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            if (getChannel.Result is not ThreadChannel threadChannel)
            {
                result.Error = true;
                result.Exception = new PNException("Retrieved channel wasn't a thread channel");
                return result;
            }
            result.Result = threadChannel;
            return result;
        }

        public async Task<ChatOperationResult> ForwardMessage(string messageTimeToken, string channelId)
        {
            var result = new ChatOperationResult();
            var getMessage = await GetMessage(channelId, messageTimeToken);
            if (result.RegisterOperation(getMessage))
            {
                return result;
            }
            result.RegisterOperation(await getMessage.Result.Forward(channelId));
            return result;
        }

        public async void AddListenerToMessagesUpdate(string channelId, List<string> messageTimeTokens,
            Action<Message> listener)
        {
            foreach (var messageTimeToken in messageTimeTokens)
            {
                var getMessage = await GetMessage(channelId, messageTimeToken);
                if (!getMessage.Error)
                {
                    getMessage.Result.OnMessageUpdated += listener;
                }
            }
        }

        public async Task<ChatOperationResult> PinMessageToChannel(string channelId, Message message)
        {
            var result = new ChatOperationResult();
            var getChannel = await GetChannel(channelId);
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            var pin = await getChannel.Result.PinMessage(message);
            result.RegisterOperation(pin);
            return result;
        }

        public async Task<ChatOperationResult> UnpinMessageFromChannel(string channelId)
        {
            var result = new ChatOperationResult();
            var getChannel = await GetChannel(channelId);
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            var unpin = await getChannel.Result.UnpinMessage();
            result.RegisterOperation(unpin);
            return result;
        }

        /// <summary>
        /// Gets the channel message history.
        /// <para>
        /// Gets the list of the messages that were sent in the channel with the provided parameters.
        /// The history is limited by the provided count of messages, start time token, and end time token.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="startTimeToken">The start time token of the messages.</param>
        /// <param name="endTimeToken">The end time token of the messages.</param>
        /// <param name="count">The maximum amount of the messages.</param>
        /// <returns>The list of the messages that were sent in the channel.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var messages = chat.GetChannelMessageHistory("channel_id", "start_time_token", "end_time_token", 10);
        /// foreach (var message in messages) {
        ///  // Message found
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="Message"/>
        public async Task<ChatOperationResult<List<Message>>> GetChannelMessageHistory(string channelId, string startTimeToken,
            string endTimeToken,
            int count)
        {
            var result = new ChatOperationResult<List<Message>>()
            {
                Result = new List<Message>()
            };
            var getHistory = await PubnubInstance.FetchHistory().Channels(new[] { channelId })
                .Start(long.Parse(startTimeToken)).End(long.Parse(endTimeToken)).MaximumPerChannel(count).IncludeMessageActions(true)
                .IncludeMeta(true).ExecuteAsync();
            if (result.RegisterOperation(getHistory) || getHistory.Result.Messages == null || !getHistory.Result.Messages.ContainsKey(channelId))
            {
                return result;
            }

            //TODO: should be in "MessageHistoryWrapper" object?
            var isMore = getHistory.Result.More != null;
            foreach (var historyItem in getHistory.Result.Messages[channelId])
            {
                if (ChatParsers.TryParseMessageFromHistory(this, channelId, historyItem, out var message))
                {
                    result.Result.Add(message);
                }
            }
            return result;
        }

        #endregion

        #region Events

        internal void BroadcastAnyEvent(ChatEvent chatEvent)
        {
            OnAnyEvent?.Invoke(chatEvent);
        }
        
        public async Task<ChatOperationResult<EventsHistoryWrapper>> GetEventsHistory(string channelId, string startTimeToken,
            string endTimeToken,
            int count)
        {
            var result = new ChatOperationResult<EventsHistoryWrapper>()
            {
                Result = new EventsHistoryWrapper()
                {
                    Events = new List<ChatEvent>()
                }
            };
            var getHistory = await PubnubInstance.FetchHistory().Channels(new[] { channelId })
                .Start(long.Parse(startTimeToken)).End(long.Parse(endTimeToken)).MaximumPerChannel(count)
                .ExecuteAsync();
            if (result.RegisterOperation(getHistory) || !getHistory.Result.Messages.ContainsKey(channelId))
            {
                return result;
            }

            var isMore = getHistory.Result.More != null;
            var events = new List<ChatEvent>();
            foreach (var message in getHistory.Result.Messages[channelId])
            {
                if (ChatParsers.TryParseEventFromHistory(this, channelId, message, out var chatEvent))
                {
                    events.Add(chatEvent);
                }
            }
            result.Result = new EventsHistoryWrapper()
            {
                Events = events,
                IsMore = isMore
            };
            return result;
        }
        
        public async Task<ChatOperationResult> EmitEvent(PubnubChatEventType type, string channelId, string jsonPayload)
        {
            var result = new ChatOperationResult();
            jsonPayload = jsonPayload.Remove(0, 1);
            jsonPayload = jsonPayload.Remove(jsonPayload.Length - 1);
            var fullPayload = $"{{{jsonPayload}, \"type\": \"{ChatEnumConverters.ChatEventTypeToString(type)}\"}}";
            result.RegisterOperation(await PubnubInstance.Publish().Channel(channelId).Message(fullPayload)
                .ExecuteAsync());
            return result;
        }

        #endregion

        public void Destroy()
        {
            PubnubInstance.Destroy();
            RateLimiter.Dispose();
        }

        ~Chat()
        {
            Destroy();
        }
    }
}