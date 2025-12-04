using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;

namespace PubnubChatApi
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
        internal const string MESSAGE_THREAD_ID_PREFIX = "PUBNUB_INTERNAL_THREAD";
        
        public Pubnub PubnubInstance { get; }
        public PubnubLogModule Logger => PubnubInstance.PNConfig.Logger;
        
        internal ChatListenerFactory ListenerFactory { get; }

        public event Action<ChatEvent> OnAnyEvent;

        public ChatAccessManager ChatAccessManager { get; }
        public MutedUsersManager MutedUsersManager { get; }
        public PubnubChatConfig Config { get; }
        internal ExponentialRateLimiter RateLimiter { get; }

        private bool storeActivity = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Chat"/> class.
        /// <para>
        /// Creates a new chat instance.
        /// </para>
        /// </summary>
        /// <param name="chatConfig">Config with Chat specific parameters</param>
        /// <param name="pubnubConfig">Config with PubNub keys and values</param>
        /// <param name="listenerFactory">Optional injectable listener factory, used in Unity to allow for dispatching Chat callbacks on main thread.</param>
        /// <returns>A ChatOperationResult containing the created Chat instance.</returns>
        /// <remarks>
        /// The constructor initializes the Chat object with a new Pubnub instance.
        /// </remarks>
        public static async Task<ChatOperationResult<Chat>> CreateInstance(PubnubChatConfig chatConfig, PNConfiguration pubnubConfig, ChatListenerFactory? listenerFactory = null)
        {
            var chat = new Chat(chatConfig, pubnubConfig, listenerFactory);
            var result = new ChatOperationResult<Chat>("Chat.CreateInstance()", chat){Result = chat};
            var getUser = await chat.GetCurrentUser().ConfigureAwait(false);
            if (getUser.Error)
            {
                result.RegisterOperation(await chat.CreateUser(chat.PubnubInstance.GetCurrentUserId()).ConfigureAwait(false));
            }
            if (chatConfig.StoreUserActivityTimestamp)
            {
                chat.StoreActivityTimeStamp();
            }
            return result;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Chat"/> class.
        /// <para>
        /// Creates a new chat instance.
        /// </para>
        /// </summary>
        /// <param name="chatConfig">Config with Chat specific parameters</param>
        /// <param name="pubnub">An existing Pubnub object instance</param>
        /// <param name="listenerFactory">Optional injectable listener factory, used in Unity to allow for dispatching Chat callbacks on main thread.</param>
        /// <returns>A ChatOperationResult containing the created Chat instance.</returns>
        /// <remarks>
        /// The constructor initializes the Chat object with an existing Pubnub instance.
        /// </remarks>
        public static async Task<ChatOperationResult<Chat>> CreateInstance(PubnubChatConfig chatConfig, Pubnub pubnub, ChatListenerFactory? listenerFactory = null)
        {
            var chat = new Chat(chatConfig, pubnub, listenerFactory);
            var result = new ChatOperationResult<Chat>("Chat.CreateInstance()", chat){Result = chat};
            var getUser = await chat.GetCurrentUser().ConfigureAwait(false);
            if (getUser.Error)
            {
                result.RegisterOperation(await chat.CreateUser(chat.PubnubInstance.GetCurrentUserId()).ConfigureAwait(false));
            }
            if (chatConfig.StoreUserActivityTimestamp)
            {
                chat.StoreActivityTimeStamp();
            }
            return result;
        }
        
        internal Chat(PubnubChatConfig chatConfig, PNConfiguration pubnubConfig, ChatListenerFactory? listenerFactory = null)
        {
            PubnubInstance = new Pubnub(pubnubConfig);
            ListenerFactory = listenerFactory ?? new DotNetListenerFactory();
            Config = chatConfig;
            ChatAccessManager = new ChatAccessManager(this);
            MutedUsersManager = new MutedUsersManager(this);
            RateLimiter = new ExponentialRateLimiter(chatConfig.RateLimitFactor);
        }
        
        internal Chat(PubnubChatConfig chatConfig, Pubnub pubnub, ChatListenerFactory? listenerFactory = null)
        {
            Config = chatConfig;
            PubnubInstance = pubnub;
            ListenerFactory = listenerFactory ?? new DotNetListenerFactory();
            ChatAccessManager = new ChatAccessManager(this);
            MutedUsersManager = new MutedUsersManager(this);
            RateLimiter = new ExponentialRateLimiter(chatConfig.RateLimitFactor);
        }
        
        #region Channels

        /// <summary>
        /// Adds a listener for channel update events on multiple channels.
        /// </summary>
        /// <param name="channelIds">List of channel IDs to listen to.</param>
        /// <param name="listener">The listener callback to invoke on channel updates.</param>
        [Obsolete("Obsolete, please use the static Channel.StreamUpdatesOn() instead")]
        public async Task AddListenerToChannelsUpdate(List<string> channelIds, Action<Channel> listener)
        {
            foreach (var channelId in channelIds)
            {
                var getResult = await GetChannel(channelId).ConfigureAwait(false);
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
        /// <returns>A ChatOperationResult containing the created Channel object.</returns>
        /// <remarks>
        /// The method creates a chat channel with the provided channel ID.
        /// </remarks>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.CreatePublicConversation("channel_id");
        /// var channel = result.Result;
        /// </code>
        /// </example>
        /// <seealso cref="Channel"/>
        public async Task<ChatOperationResult<Channel>> CreatePublicConversation(string channelId = "")
        {
            if (string.IsNullOrEmpty(channelId))
            {
                channelId = Guid.NewGuid().ToString();
            }

            return await CreatePublicConversation(channelId, new ChatChannelData()).ConfigureAwait(false);
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
        /// <returns>A ChatOperationResult containing the created Channel object.</returns>
        /// <remarks>
        /// The method creates a chat channel with the provided channel ID.
        /// </remarks>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.CreatePublicConversation("channel_id");
        /// var channel = result.Result;
        /// </code>
        /// </example>
        /// <seealso cref="Channel"/>
        /// <seealso cref="ChatChannelData"/>
        public async Task<ChatOperationResult<Channel>> CreatePublicConversation(string channelId, ChatChannelData additionalData)
        {
            var result = new ChatOperationResult<Channel>("Chat.CreatePublicConversation()", this);
            var existingChannel = await GetChannel(channelId).ConfigureAwait(false);
            if (!result.RegisterOperation(existingChannel))
            {
                Logger.Debug("Trying to create a channel with ID that already exists! Returning existing one.");
                result.Result = existingChannel.Result;
                return result;
            }

            additionalData.Type = "public";
            var updated = await Channel.UpdateChannelData(this, channelId, additionalData).ConfigureAwait(false);
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
            var result = new ChatOperationResult<CreatedChannelWrapper>($"Chat.CreateConversation-{type}", this){Result = new CreatedChannelWrapper()};
            
            if (string.IsNullOrEmpty(channelId))
            {
                channelId = Guid.NewGuid().ToString();
            }
            
            var existingChannel = await GetChannel(channelId).ConfigureAwait(false);
            if (!result.RegisterOperation(existingChannel))
            {
                Logger.Debug("Trying to create a channel with ID that already exists! Returning existing one.");
                result.Result.CreatedChannel = existingChannel.Result;
                return result;
            }
            
            channelData ??= new ChatChannelData();
            channelData.Type = type;
            var updated = await Channel.UpdateChannelData(this, channelId, channelData).ConfigureAwait(false);
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
                .ExecuteAsync().ConfigureAwait(false);

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
                var inviteMembership = await InviteToChannel(channelId, users[0].Id).ConfigureAwait(false);
                if (result.RegisterOperation(inviteMembership))
                {
                    return result;
                }
                result.Result.InviteesMemberships = new List<Membership>() { inviteMembership.Result };
            }else if (type == "group")
            {
                var inviteMembership = await InviteMultipleToChannel(channelId, users).ConfigureAwait(false);
                if (result.RegisterOperation(inviteMembership))
                {
                    return result;
                }
                result.Result.InviteesMemberships = new List<Membership>(inviteMembership.Result);
            }
            return result;
        }

        /// <summary>
        /// Creates a direct conversation between the current user and the specified user.
        /// </summary>
        /// <param name="user">The user to create a direct conversation with.</param>
        /// <param name="channelId">Optional channel ID. If not provided, a new GUID will be used.</param>
        /// <param name="channelData">Optional additional channel data.</param>
        /// <param name="membershipData">Optional membership data for the conversation.</param>
        /// <returns>A ChatOperationResult containing the created channel wrapper with channel and membership information.</returns>
        public async Task<ChatOperationResult<CreatedChannelWrapper>> CreateDirectConversation(User user, string channelId = "",
            ChatChannelData? channelData = null, ChatMembershipData? membershipData = null)
        {
            return await CreateConversation("direct", new List<User>() { user }, channelId, channelData,
                membershipData).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a group conversation with multiple users.
        /// </summary>
        /// <param name="users">The list of users to include in the group conversation.</param>
        /// <param name="channelId">Optional channel ID. If not provided, a new GUID will be used.</param>
        /// <param name="channelData">Optional additional channel data.</param>
        /// <param name="membershipData">Optional membership data for the conversation.</param>
        /// <returns>A ChatOperationResult containing the created channel wrapper with channel and membership information.</returns>
        public async Task<ChatOperationResult<CreatedChannelWrapper>> CreateGroupConversation(List<User> users, string channelId = "",
            ChatChannelData? channelData = null, ChatMembershipData? membershipData = null)
        {
            return await CreateConversation("group", users, channelId, channelData,
                membershipData).ConfigureAwait(false);
        }

        /// <summary>
        /// Invites a user to a channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel to invite the user to.</param>
        /// <param name="userId">The ID of the user to invite.</param>
        /// <returns>A ChatOperationResult containing the created membership for the invited user.</returns>
        public async Task<ChatOperationResult<Membership>> InviteToChannel(string channelId, string userId)
        {
            var result = new ChatOperationResult<Membership>("Chat.InviteToChannel()", this);
            //Check if already a member first
            var members = await GetChannelMemberships(channelId, filter:$"uuid.id == \"{userId}\"").ConfigureAwait(false);
            if (!result.RegisterOperation(members) && members.Result.Memberships.Any())
            {
                //Already a member, just return current membership
                result.Result = members.Result.Memberships[0];
                return result;
            }
            
            var channel = await GetChannel(channelId).ConfigureAwait(false);
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
                    Status = "pending"
                    //TODO: these too here?
                    //TODO: again, should ChatMembershipData from Create(...)Channel also be passed here?
                    /*Custom = ,
                    Type = */
                }
            }).ExecuteAsync().ConfigureAwait(false);

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
            await EmitEvent(PubnubChatEventType.Invite, userId, inviteEventPayload).ConfigureAwait(false);
            
            var newMembership = new Membership(this, userId, channelId, new ChatMembershipData()
            {
                Status = "pending"
            });
            await newMembership.SetLastReadMessageTimeToken(ChatUtils.TimeTokenNow()).ConfigureAwait(false);

            result.Result = newMembership;
            return result;
        }

        /// <summary>
        /// Invites multiple users to a channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel to invite users to.</param>
        /// <param name="users">The list of users to invite.</param>
        /// <returns>A ChatOperationResult containing a list of created memberships for the invited users.</returns>
        public async Task<ChatOperationResult<List<Membership>>> InviteMultipleToChannel(string channelId, List<User> users)
        {
            var result = new ChatOperationResult<List<Membership>>("Chat.InviteMultipleToChannel()", this) { Result = new List<Membership>() };
            var channel = await GetChannel(channelId).ConfigureAwait(false);
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
                .Uuids(users.Select(x => new PNChannelMember() { Custom = x.CustomData, Uuid = x.Id, Status = "pending"}).ToList())
                .ExecuteAsync().ConfigureAwait(false);
            
            if (result.RegisterOperation(inviteResponse))
            {
                return result;
            }
            
            foreach (var channelMember in inviteResponse.Result.ChannelMembers)
            {
                var userId = channelMember.UuidMetadata.Uuid;
                if (!users.Any(x => x.Id == userId))
                {
                    continue;
                }
                var newMembership = new Membership(this, userId, channelId, channelMember);
                await newMembership.SetLastReadMessageTimeToken(ChatUtils.TimeTokenNow()).ConfigureAwait(false);
                result.Result.Add(newMembership);
                
                var inviteEventPayload = $"{{\"channelType\": \"{channel.Result.Type}\", \"channelId\": {channelId}}}";
                await EmitEvent(PubnubChatEventType.Invite, userId, inviteEventPayload).ConfigureAwait(false);
            }

            await channel.Result.Refresh().ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Performs an async retrieval of a Channel object with a given ID.
        /// </summary>
        /// <param name="channelId">ID of the channel.</param>
        /// <returns>A ChatOperationResult containing the Channel object if it exists, null otherwise.</returns>
        public async Task<ChatOperationResult<Channel>> GetChannel(string channelId)
        {
            var result = new ChatOperationResult<Channel>("Chat.GetChannel()", this);
            var getResult = await Channel.GetChannelData(this, channelId).ConfigureAwait(false);
            if (result.RegisterOperation(getResult))
            {
                return result;
            }
            if (channelId.Contains(MESSAGE_THREAD_ID_PREFIX) 
                && getResult.Result.Custom.TryGetValue("parentChannelId", out var parentChannelId)
                && getResult.Result.Custom.TryGetValue("parentMessageTimetoken", out var parentMessageTimeToken))
            {
                result.Result = new ThreadChannel(this, channelId, parentChannelId.ToString(), parentMessageTimeToken.ToString(), getResult.Result);
            }
            else
            {
                result.Result = new Channel(this, channelId, getResult.Result);
            }
            return result;
        }

        /// <summary>
        /// Gets the list of channels with the provided parameters.
        /// </summary>
        /// <param name="filter">Filter criteria for channels.</param>
        /// <param name="sort">Sort criteria for channels.</param>
        /// <param name="limit">The maximum number of channels to get.</param>
        /// <param name="page">Pagination object for retrieving specific page results.</param>
        /// <returns>A wrapper containing the list of channels and pagination information.</returns>
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
            var response = await operation.ExecuteAsync().ConfigureAwait(false);
            
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
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.UpdateChannel("channel_id", new ChatChannelData {
        ///    ChannelName = "new_name"
        ///    // ...
        ///  });
        /// </code>
        /// </example>
        /// <seealso cref="ChatChannelData"/>
        public async Task<ChatOperationResult> UpdateChannel(string channelId, ChatChannelData updatedData)
        {
            var result = new ChatOperationResult("Chat.UpdateChannel()", this);
            result.RegisterOperation(await Channel.UpdateChannelData(this, channelId, updatedData).ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Deletes the channel with the provided channel ID.
        /// <para>
        /// The channel is deleted with all the messages and users.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="soft">Bool specifying the type of deletion.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.DeleteChannel("channel_id", true);
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> DeleteChannel(string channelId, bool soft = false)
        {
            var result = new ChatOperationResult("Chat.DeleteChannel()", this);
            if (!soft)
            {
                result.RegisterOperation(await PubnubInstance.RemoveChannelMetadata().Channel(channelId).ExecuteAsync().ConfigureAwait(false));
            }
            else
            {
                var data = await Channel.GetChannelData(this, channelId).ConfigureAwait(false);
                if (result.RegisterOperation(data))
                {
                    return result;
                }
                var channelData = (ChatChannelData)data.Result;
                channelData.CustomData ??= new Dictionary<string, object>();
                channelData.CustomData["deleted"] = true;
                var updateResult = await Channel.UpdateChannelData(this, channelId, channelData).ConfigureAwait(false);
                result.RegisterOperation(updateResult);
            }
            return result;
        }

        #endregion

        #region Users

        internal async void StoreActivityTimeStamp()
        {
            var currentUserId = PubnubInstance.GetCurrentUserId();
            storeActivity = true;
            while (storeActivity)
            {
                var getResult = await User.GetUserData(this, currentUserId).ConfigureAwait(false);
                var data = (ChatUserData)getResult.Result;
                if (getResult.Status.Error)
                {
                    Logger.Error($"Error when trying to store user activity timestamp: {getResult.Status.ErrorData}");
                    await Task.Delay(Config.StoreUserActivityInterval).ConfigureAwait(false);
                    continue;
                }
                data.CustomData ??= new Dictionary<string, object>();
                data.CustomData["lastActiveTimestamp"] = ChatUtils.TimeTokenNow();
                var setData = await User.UpdateUserData(this, currentUserId, data).ConfigureAwait(false);
                if (setData.Status.Error)
                {
                    Logger.Error($"Error when trying to store user activity timestamp: {setData.Status.ErrorData}");
                }
                await Task.Delay(Config.StoreUserActivityInterval).ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// Gets the current user's mentions within a specified time range.
        /// </summary>
        /// <param name="startTimeToken">The start time token for the search range.</param>
        /// <param name="endTimeToken">The end time token for the search range.</param>
        /// <param name="count">The maximum number of mentions to retrieve.</param>
        /// <returns>A ChatOperationResult containing the user mentions wrapper with mention data.</returns>
        public async Task<ChatOperationResult<UserMentionsWrapper>> GetCurrentUserMentions(string startTimeToken, string endTimeToken,
            int count)
        {
            var result = new ChatOperationResult<UserMentionsWrapper>("Chat.GetCurrentUserMentions()", this);
            var id = PubnubInstance.GetCurrentUserId();
            var getEventHistory = await GetEventsHistory(id, startTimeToken, endTimeToken, count).ConfigureAwait(false);
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
                var getMessage = await GetMessage(mentionChannel.ToString(), messageTimeToken.ToString()).ConfigureAwait(false);
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
        /// <returns>A ChatOperationResult containing the current User object if there is one, null otherwise.</returns>
        public async Task<ChatOperationResult<User>> GetCurrentUser()
        {
            var result = new ChatOperationResult<User>("Chat.GetCurrentUser()", this);
            var userId = PubnubInstance.GetCurrentUserId();
            var getUser = await GetUser(userId).ConfigureAwait(false);
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
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var result = await chat.SetRestriction("user_id", "channel_id", true, true, "Spamming");
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> SetRestriction(string userId, string channelId, bool banUser, bool muteUser, string reason)
        {
            var result = new ChatOperationResult("Chat.SetRestriction()", this);
            var restrictionsChannelId = $"{INTERNAL_MODERATION_PREFIX}_{channelId}";
            var getResult = await Channel.GetChannelData(this, restrictionsChannelId).ConfigureAwait(false);
            if (result.RegisterOperation(getResult))
            {
                if (result.RegisterOperation(await Channel.UpdateChannelData(this, restrictionsChannelId,
                        new ChatChannelData()).ConfigureAwait(false)))
                {
                    return result;
                }
            }
            var moderationEventsChannelId = INTERNAL_MODERATION_PREFIX + userId;
            //Lift restrictions
            if (!banUser && !muteUser)
            {
                if (result.RegisterOperation(await PubnubInstance.RemoveChannelMembers().Channel(restrictionsChannelId)
                        .Uuids(new List<string>() { userId }).ExecuteAsync().ConfigureAwait(false)))
                {
                    return result;
                }
                result.RegisterOperation(await EmitEvent(PubnubChatEventType.Moderation, moderationEventsChannelId,
                    $"{{\"channelId\": \"{channelId}\", \"restriction\": \"lifted\", \"reason\": \"{reason}\"}}").ConfigureAwait(false));
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
                }).ExecuteAsync().ConfigureAwait(false)))
            {
                return result;
            }
            result.RegisterOperation(await EmitEvent(PubnubChatEventType.Moderation, moderationEventsChannelId,
                $"{{\"channelId\": \"{channelId}\", \"restriction\": \"{(banUser ? "banned" : "muted")}\", \"reason\": \"{reason}\"}}").ConfigureAwait(false));
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
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var result = await chat.SetRestriction("user_id", "channel_id", new Restriction(){Ban = true, Mute = true, Reason = "Spamming"});
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> SetRestriction(string userId, string channelId, Restriction restriction)
        {
            return await SetRestriction(userId, channelId, restriction.Ban, restriction.Mute, restriction.Reason).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a listener for user update events on multiple users.
        /// </summary>
        /// <param name="userIds">List of user IDs to listen to.</param>
        /// <param name="listener">The listener callback to invoke on user updates.</param>
        [Obsolete("Obsolete, please use the static User.StreamUpdatesOn() instead")]
        public async void AddListenerToUsersUpdate(List<string> userIds, Action<User> listener)
        {
            foreach (var userId in userIds)
            {
                var getUser = await GetUser(userId).ConfigureAwait(false);
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
        /// <returns>A ChatOperationResult containing the created User object.</returns>
        /// <remarks>
        /// The data for user is empty.
        /// </remarks>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.CreateUser("user_id");
        /// var user = result.Result;
        /// </code>
        /// </example>
        /// <seealso cref="User"/>
        public async Task<ChatOperationResult<User>> CreateUser(string userId)
        {
            return await CreateUser(userId, new ChatUserData()).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new user with the provided user ID.
        /// <para>
        /// Creates a new user with the provided data and the provided user ID.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="additionalData">The additional data for the user.</param>
        /// <returns>A ChatOperationResult containing the created User object.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.CreateUser("user_id");
        /// var user = result.Result;
        /// </code>
        /// </example>
        /// <seealso cref="User"/>
        public async Task<ChatOperationResult<User>> CreateUser(string userId, ChatUserData additionalData)
        {
            var result = new ChatOperationResult<User>("Chat.CreateUser()", this);
            var existingUser = await GetUser(userId).ConfigureAwait(false);
            if (!result.RegisterOperation(existingUser))
            {
                result.Result = existingUser.Result;
                return result;
            }
            
            var update = await User.UpdateUserData(this, userId, additionalData).ConfigureAwait(false);
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
        /// <returns>A ChatOperationResult containing true if the user is present, false otherwise.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.IsPresent("user_id", "channel_id");
        /// if (result.Result) {
        ///   // User is present 
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="WhoIsPresent"/>
        /// <seealso cref="WherePresent"/>
        public async Task<ChatOperationResult<bool>> IsPresent(string userId, string channelId)
        {
            var result = new ChatOperationResult<bool>("Chat.IsPresent()", this);
            var getChannel = await GetChannel(channelId).ConfigureAwait(false);
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            var isPresent = await getChannel.Result.IsUserPresent(userId).ConfigureAwait(false);
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
        /// <returns>A ChatOperationResult containing the list of user IDs present in the channel.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.WhoIsPresent("channel_id");
        /// foreach (var userId in result.Result) {
        ///   // User is present on the channel
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="WherePresent"/>
        /// <seealso cref="IsPresent"/>
        public async Task<ChatOperationResult<List<string>>> WhoIsPresent(string channelId)
        {
            var result = new ChatOperationResult<List<string>>("Chat.WhoIsPresent()", this) { Result = new List<string>() };
            var getChannel = await GetChannel(channelId).ConfigureAwait(false);
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            var whoIs = await getChannel.Result.WhoIsPresent().ConfigureAwait(false);
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
        /// <returns>A ChatOperationResult containing the list of channel IDs where the user is present.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.WherePresent("user_id");
        /// foreach (var channelId in result.Result) {
        ///  // Channel where User is present
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="WhoIsPresent"/>
        /// <seealso cref="IsPresent"/>
        public async Task<ChatOperationResult<List<string>>> WherePresent(string userId)
        {
            var result = new ChatOperationResult<List<string>>("Chat.WherePresent()", this) { Result = new List<string>() };
            var getUser = await GetUser(userId).ConfigureAwait(false);
            if (result.RegisterOperation(getUser))
            {
                return result;
            }
            var wherePresent = await getUser.Result.WherePresent().ConfigureAwait(false);
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
        /// <returns>A ChatOperationResult containing the User object if one with given ID is found, null otherwise.</returns>
        public async Task<ChatOperationResult<User>> GetUser(string userId)
        {
            var result = new ChatOperationResult<User>("Chat.GetUser()", this);
            var getData = await User.GetUserData(this, userId).ConfigureAwait(false);
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
        /// <param name="filter">Filter criteria for users.</param>
        /// <param name="sort">Sort criteria for users.</param>
        /// <param name="limit">The maximum number of users to get.</param>
        /// <param name="page">Pagination object for retrieving specific page results.</param>
        /// <returns>The list of the users that matches the provided parameters.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var users = await chat.GetUsers(
        ///     filter: "status == 'admin'",
        ///     limit: 10
        /// );
        /// foreach (var user in users.Users) {
        ///  // User found
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="User"/>
        public async Task<ChatOperationResult<UsersResponseWrapper>> GetUsers(string filter = "", string sort = "", int limit = 0,
            PNPageObject page = null)
        {
            var result = new ChatOperationResult<UsersResponseWrapper>("Chat.GetUsers()", this);
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
            var getUuidMetadata = await operation.ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(getUuidMetadata))
            {
                return result;
            }
            var response = new UsersResponseWrapper()
            {
                Users = new List<User>(),
                Total = getUuidMetadata.Result.TotalCount,
                Page = result.Result.Page
            };
            foreach (var resultMetadata in getUuidMetadata.Result.Uuids)
            {
                var user = new User(this, resultMetadata.Uuid, resultMetadata);
                response.Users.Add(user);
            }
            result.Result = response;
            return result;
        }
        
        /// <summary>
        /// Updates the user with the provided user ID.
        /// <para>
        /// Updates the user with the provided user ID with the provided data.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="updatedData">The updated data for the user.</param>
        /// <returns>A ChatOperationResult with information on the Update's success. </returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.UpdateUser("user_id", new ChatUserData {
        ///   Username = "new_name"
        ///   // ...
        /// });
        /// </code>
        /// </example>
        /// <seealso cref="ChatUserData"/>
        public async Task<ChatOperationResult> UpdateUser(string userId, ChatUserData updatedData)
        {
            return (await User.UpdateUserData(this, userId, updatedData).ConfigureAwait(false)).ToChatOperationResult("Chat.UpdateUser()", this);
        }

        /// <summary>
        /// Deletes the user with the provided user ID.
        /// <para>
        /// The user is deleted with all the messages and channels.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="soft">Bool specifying the type of deletion.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.DeleteUser("user_id");
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> DeleteUser(string userId, bool soft = false)
        {
            var result = new ChatOperationResult("Chat.DeleteUser()", this);
            if (!soft)
            {
                result.RegisterOperation(await PubnubInstance.RemoveUuidMetadata().Uuid(userId).ExecuteAsync().ConfigureAwait(false));
            }
            else
            {
                var data = await User.GetUserData(this, userId).ConfigureAwait(false);
                if (result.RegisterOperation(data))
                {
                    return result;
                }
                var userData = (ChatUserData)data.Result;
                userData.CustomData ??= new Dictionary<string, object>();
                userData.CustomData["deleted"] = true;
                var updateResult =  await User.UpdateUserData(this, userId, userData).ConfigureAwait(false);
                result.RegisterOperation(updateResult);
            }
            return result;
        }

        #endregion

        #region Memberships

        /// <summary>
        /// Gets the memberships of the user with the provided user ID.
        /// <para>
        /// Gets all the memberships of the user with the provided user ID.
        /// The memberships can be filtered, sorted, and paginated.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="filter">Filter criteria for memberships.</param>
        /// <param name="sort">Sort criteria for memberships.</param>
        /// <param name="limit">The maximum number of memberships to retrieve.</param>
        /// <param name="page">Pagination object for retrieving specific page results.</param>
        /// <returns>A ChatOperationResult containing the list of the memberships of the user.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.GetUserMemberships(
        ///         "user_id",
        ///         limit: 10
        /// );
        /// foreach (var membership in result.Result.Memberships) {
        ///  // Membership found
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="Membership"/>
        public async Task<ChatOperationResult<MembersResponseWrapper>> GetUserMemberships(string userId, string filter = "",
            string sort = "",
            int limit = 0, PNPageObject page = null)
        {
            var result = new ChatOperationResult<MembersResponseWrapper>("Chat.GetUserMemberships()", this);
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
            var getMemberships = await operation.ExecuteAsync().ConfigureAwait(false);
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
        /// The memberships can be filtered, sorted, and paginated.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="filter">Filter criteria for memberships.</param>
        /// <param name="sort">Sort criteria for memberships.</param>
        /// <param name="limit">The maximum number of memberships to retrieve.</param>
        /// <param name="page">Pagination object for retrieving specific page results.</param>
        /// <returns>A ChatOperationResult containing the list of the memberships of the channel.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.GetChannelMemberships(
        ///         "channel_id",
        ///         limit: 10
        /// );
        /// foreach (var membership in result.Result.Memberships) {
        ///  // Membership found
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="Membership"/>
        public async Task<ChatOperationResult<MembersResponseWrapper>> GetChannelMemberships(string channelId, string filter = "",
            string sort = "",
            int limit = 0, PNPageObject page = null)
        {
            var result = new ChatOperationResult<MembersResponseWrapper>("Chat.GetChannelMemberships()", this);
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

            var getResult = await operation.ExecuteAsync().ConfigureAwait(false);
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
        
        /// <summary>
        /// Gets the message reports history for a specific channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel to get reports for.</param>
        /// <param name="startTimeToken">The start time token for the history range.</param>
        /// <param name="endTimeToken">The end time token for the history range.</param>
        /// <param name="count">The maximum number of reports to retrieve.</param>
        /// <returns>A ChatOperationResult containing the events history wrapper with report events.</returns>
        public async Task<ChatOperationResult<EventsHistoryWrapper>> GetMessageReportsHistory(string channelId, string startTimeToken,
            string endTimeToken, int count)
        {
            return await GetEventsHistory($"PUBNUB_INTERNAL_MODERATION_{channelId}", startTimeToken, endTimeToken,
                count).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously gets the <c>Message</c> object for the given timetoken.
        /// </summary>
        /// <param name="channelId">ID of the channel on which the message was sent.</param>
        /// <param name="messageTimeToken">TimeToken of the searched-for message.</param>
        /// <returns>A ChatOperationResult containing the Message object if one was found, null otherwise.</returns>
        public async Task<ChatOperationResult<Message>> GetMessage(string channelId, string messageTimeToken)
        {
            var result = new ChatOperationResult<Message>("Chat.GetMessage()", this);
            var startTimeToken = (long.Parse(messageTimeToken) + 1).ToString();
            var getHistory = await GetChannelMessageHistory(channelId, startTimeToken, messageTimeToken, 1).ConfigureAwait(false);
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

        /// <summary>
        /// Marks all messages as read for the current user across all their channels.
        /// </summary>
        /// <param name="filter">Optional filter to apply when getting user memberships.</param>
        /// <param name="sort">Optional sort criteria for memberships.</param>
        /// <param name="limit">Maximum number of memberships to process (0-100).</param>
        /// <param name="page">Optional pagination object.</param>
        /// <returns>A ChatOperationResult containing the wrapper with updated memberships and status information.</returns>
        public async Task<ChatOperationResult<MarkMessagesAsReadWrapper>> MarkAllMessagesAsRead(string filter = "", string sort = "",
            int limit = 0,
            PNPageObject page = null)
        {
            var result = new ChatOperationResult<MarkMessagesAsReadWrapper>("Chat.MarkAllMessagesAsRead()", this);
            if (limit < 0 || limit > 100)
            {
                result.Error = true;
                result.Exception = new PNException("For marking messages as read limit has to be between 0 and 100");
                return result;
            }
            var currentUserId = PubnubInstance.GetCurrentUserId();
            var getCurrentUser = await GetCurrentUser().ConfigureAwait(false);
            if (result.RegisterOperation(getCurrentUser))
            {
                return result;
            }
            var getCurrentMemberships = await getCurrentUser.Result.GetMemberships(filter, sort, limit, page).ConfigureAwait(false);
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
                membership.MembershipData.CustomData ??= new(); 
                membership.MembershipData.CustomData["lastReadMessageTimetoken"] = timeToken;
            }
            if (result.RegisterOperation(await Membership.UpdateMembershipsData(this, currentUserId, memberships).ConfigureAwait(false)))
            {
                return result;
            }
            foreach (var membership in memberships)
            {
                await EmitEvent(PubnubChatEventType.Receipt, membership.ChannelId,
                    $"{{\"messageTimetoken\": \"{timeToken}\"}}").ConfigureAwait(false);
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
        
        /// <summary>
        /// Gets unread message counts for the current user's channels.
        /// </summary>
        /// <param name="filter">Optional filter to apply when getting user memberships.</param>
        /// <param name="sort">Optional sort criteria for memberships.</param>
        /// <param name="limit">Maximum number of memberships to process (0-100).</param>
        /// <param name="page">Optional pagination object.</param>
        /// <returns>A ChatOperationResult containing a list of unread message wrappers with count information per channel.</returns>
        public async Task<ChatOperationResult<List<UnreadMessageWrapper>>> GetUnreadMessagesCounts(string filter = "", string sort = "",
            int limit = 0,
            PNPageObject page = null)
        {
            var result = new ChatOperationResult<List<UnreadMessageWrapper>>("Chat.GetUnreadMessagesCounts()", this);
            if (limit < 0 || limit > 100)
            {
                result.Error = true;
                result.Exception = new PNException("For getting message counts limit has to be between 0 and 100");
                return result;
            }
            var getCurrentUser = await GetCurrentUser().ConfigureAwait(false);
            if (result.RegisterOperation(getCurrentUser))
            {
                return result;
            }
            var getCurrentMemberships = await getCurrentUser.Result.GetMemberships(filter, sort, limit, page).ConfigureAwait(false);
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
            var getCounts = await PubnubInstance.MessageCounts().Channels(channelIds.ToArray()).ChannelsTimetoken(timeTokens.ToArray())
                .ExecuteAsync().ConfigureAwait(false);
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

        /// <summary>
        /// Creates a thread channel for a specific message.
        /// </summary>
        /// <param name="messageTimeToken">The time token of the message to create a thread for.</param>
        /// <param name="messageChannelId">The ID of the channel where the message was sent.</param>
        /// <returns>A ChatOperationResult containing the created ThreadChannel.</returns>
        public async Task<ChatOperationResult<ThreadChannel>> CreateThreadChannel(string messageTimeToken, string messageChannelId)
        {
            var result = new ChatOperationResult<ThreadChannel>("Chat.CreateThreadChannel()", this);
            var getMessage = await GetMessage(messageChannelId, messageTimeToken).ConfigureAwait(false);
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

        /// <summary>
        /// Removes a thread channel associated with a specific message.
        /// </summary>
        /// <param name="messageTimeToken">The time token of the message whose thread to remove.</param>
        /// <param name="messageChannelId">The ID of the channel where the message was sent.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> RemoveThreadChannel(string messageTimeToken, string messageChannelId)
        {
            var result = new ChatOperationResult("Chat.RemoveThreadChannel()", this);
            var getMessage = await GetMessage(messageChannelId, messageTimeToken).ConfigureAwait(false);
            if (result.RegisterOperation(getMessage))
            {
                return result;
            }
            result.RegisterOperation(await getMessage.Result.RemoveThread().ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Asynchronously tries to retrieve a ThreadChannel object from a Message object if there is one.
        /// </summary>
        /// <param name="message">Message on which the ThreadChannel is supposed to be.</param>
        /// <returns>A ChatOperationResult containing the ThreadChannel object if one was found, null otherwise.</returns>
        public async Task<ChatOperationResult<ThreadChannel>> GetThreadChannel(Message message)
        {
            var result = new ChatOperationResult<ThreadChannel>("Chat.GetThreadChannel()", this);
            var getChannel = await GetChannel(message.GetThreadId()).ConfigureAwait(false);
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

        /// <summary>
        /// Forwards a message to a different channel.
        /// </summary>
        /// <param name="messageTimeToken">The time token of the message to forward.</param>
        /// <param name="channelId">The ID of the channel to forward the message to.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> ForwardMessage(string messageTimeToken, string channelId)
        {
            var result = new ChatOperationResult("Chat.ForwardMessage()", this);
            var getMessage = await GetMessage(channelId, messageTimeToken).ConfigureAwait(false);
            if (result.RegisterOperation(getMessage))
            {
                return result;
            }
            result.RegisterOperation(await getMessage.Result.Forward(channelId).ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Adds a listener for message update events on specific messages in a channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel containing the messages.</param>
        /// <param name="messageTimeTokens">List of message time tokens to listen to for updates.</param>
        /// <param name="listener">The listener callback to invoke on message updates.</param>
        [Obsolete("Obsolete, please use the static Message.StreamUpdatesOn() instead")]
        public async void AddListenerToMessagesUpdate(string channelId, List<string> messageTimeTokens,
            Action<Message> listener)
        {
            foreach (var messageTimeToken in messageTimeTokens)
            {
                var getMessage = await GetMessage(channelId, messageTimeToken).ConfigureAwait(false);
                if (!getMessage.Error)
                {
                    getMessage.Result.OnMessageUpdated += listener;
                }
            }
        }

        /// <summary>
        /// Pins a message to a channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel to pin the message to.</param>
        /// <param name="message">The message to pin.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> PinMessageToChannel(string channelId, Message message)
        {
            var result = new ChatOperationResult("Chat.PinMessageToChannel()", this);
            var getChannel = await GetChannel(channelId).ConfigureAwait(false);
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            var pin = await getChannel.Result.PinMessage(message).ConfigureAwait(false);
            result.RegisterOperation(pin);
            return result;
        }

        /// <summary>
        /// Unpins the currently pinned message from a channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel to unpin the message from.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> UnpinMessageFromChannel(string channelId)
        {
            var result = new ChatOperationResult("Chat.UnPinMessageFromChannel()", this);
            var getChannel = await GetChannel(channelId).ConfigureAwait(false);
            if (result.RegisterOperation(getChannel))
            {
                return result;
            }
            var unpin = await getChannel.Result.UnpinMessage().ConfigureAwait(false);
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
        /// <returns>A ChatOperationResult containing the list of messages that were sent in the channel.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var result = await chat.GetChannelMessageHistory("channel_id", "start_time_token", "end_time_token", 10);
        /// foreach (var message in result.Result) {
        ///  // Message found
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="Message"/>
        public async Task<ChatOperationResult<List<Message>>> GetChannelMessageHistory(string channelId, string startTimeToken,
            string endTimeToken,
            int count)
        {
            var result = new ChatOperationResult<List<Message>>("Chat.GetChannelMessageHistory()", this)
            {
                Result = new List<Message>()
            };
            var getHistory = await PubnubInstance.FetchHistory().Channels(new[] { channelId })
                .Start(long.Parse(startTimeToken)).End(long.Parse(endTimeToken)).MaximumPerChannel(count).IncludeMessageActions(true)
                .IncludeMeta(true).ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(getHistory) || getHistory.Result.Messages == null || !getHistory.Result.Messages.ContainsKey(channelId))
            {
                return result;
            }

            //TODO: should be in "MessageHistoryWrapper" object?
            var isMore = getHistory.Result.More != null;
            foreach (var historyItem in getHistory.Result.Messages[channelId])
            {
                if (ChatParsers.TryParseMessageFromHistory(this, channelId, historyItem, out var message) 
                    && !MutedUsersManager.MutedUsers.Contains(message.UserId))
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
        
        /// <summary>
        /// Gets the events history for a specific channel within a time range.
        /// </summary>
        /// <param name="channelId">The ID of the channel to get events for.</param>
        /// <param name="startTimeToken">The start time token for the history range.</param>
        /// <param name="endTimeToken">The end time token for the history range.</param>
        /// <param name="count">The maximum number of events to retrieve.</param>
        /// <returns>A ChatOperationResult containing the events history wrapper with chat events.</returns>
        public async Task<ChatOperationResult<EventsHistoryWrapper>> GetEventsHistory(string channelId, string startTimeToken,
            string endTimeToken,
            int count)
        {
            var result = new ChatOperationResult<EventsHistoryWrapper>("Chat.GetEventsHistory()", this)
            {
                Result = new EventsHistoryWrapper()
                {
                    Events = new List<ChatEvent>()
                }
            };
            var getHistory = await PubnubInstance.FetchHistory().Channels(new[] { channelId })
                .Start(long.Parse(startTimeToken)).End(long.Parse(endTimeToken)).MaximumPerChannel(count)
                .ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(getHistory) || !getHistory.Result.Messages.ContainsKey(channelId))
            {
                return result;
            }

            var isMore = getHistory.Result.More != null;
            var events = new List<ChatEvent>();
            foreach (var message in getHistory.Result.Messages[channelId])
            {
                if (ChatParsers.TryParseEventFromHistory(this, channelId, message, out var chatEvent) 
                    && !MutedUsersManager.MutedUsers.Contains(chatEvent.UserId))
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
        
        /// <summary>
        /// Emits a chat event on the specified channel.
        /// </summary>
        /// <param name="type">The type of event to emit.</param>
        /// <param name="channelId">The channel ID where to emit the event.</param>
        /// <param name="jsonPayload">The JSON payload of the event.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        public async Task<ChatOperationResult> EmitEvent(PubnubChatEventType type, string channelId, string jsonPayload)
        {
            var result = new ChatOperationResult("Chat.EmitEvent()", this);
            jsonPayload = jsonPayload.Remove(0, 1);
            jsonPayload = jsonPayload.Remove(jsonPayload.Length - 1);
            var fullPayload = $"{{{jsonPayload}, \"type\": \"{ChatEnumConverters.ChatEventTypeToString(type)}\"}}";
            var emitOperation = PubnubInstance.Publish().Channel(channelId).Message(fullPayload);
            if (type is PubnubChatEventType.Receipt or PubnubChatEventType.Typing)
            {
                emitOperation.ShouldStore(false);
            }
            result.RegisterOperation(await emitOperation.ExecuteAsync().ConfigureAwait(false));
            return result;
        }

        #endregion

        #region Push
        
        /// <summary>
        /// Retrieves the Push Notifications config from the main Chat config.
        /// Alternatively you can also use Config.PushNotifications
        /// </summary>
        public PubnubChatConfig.PushNotificationsConfig GetCommonPushOptions => Config.PushNotifications;
        
        /// <summary>
        /// Registers a list of channels to receive push notifications.
        /// </summary>
        public async Task<ChatOperationResult> RegisterPushChannels(List<string> channelIds)
        {
            var pushSettings = GetCommonPushOptions;
            return (await PubnubInstance.AddPushNotificationsOnChannels()
                    .Channels(channelIds.ToArray())
                    .PushType(pushSettings.DeviceGateway)
                    .DeviceId(pushSettings.DeviceToken)
                    .Topic(pushSettings.APNSTopic)
                    .Environment(pushSettings.APNSEnvironment)
                    .ExecuteAsync()
                    .ConfigureAwait(false))
                .ToChatOperationResult("Chat.RegisterPushChannels()", this);
        }
        
        /// <summary>
        /// Un-registers a list of channels from receiving push notifications.
        /// </summary>
        public async Task<ChatOperationResult> UnRegisterPushChannels(List<string> channelIds)
        {
            var pushSettings = GetCommonPushOptions;
            return (await PubnubInstance.RemovePushNotificationsFromChannels()
                    .Channels(channelIds.ToArray())
                    .PushType(pushSettings.DeviceGateway)
                    .DeviceId(pushSettings.DeviceToken)
                    .Topic(pushSettings.APNSTopic)
                    .Environment(pushSettings.APNSEnvironment)
                    .ExecuteAsync()
                    .ConfigureAwait(false))
                .ToChatOperationResult("Chat.RegisterPushChannels()", this);
        }
        
        /// <summary>
        /// Un-registers all channels from receiving push notifications.
        /// </summary>
        public async Task<ChatOperationResult> UnRegisterAllPushChannels()
        {
            var pushSettings = GetCommonPushOptions;
            return (await PubnubInstance.RemoveAllPushNotificationsFromDeviceWithPushToken()
                    .PushType(pushSettings.DeviceGateway)
                    .DeviceId(pushSettings.DeviceToken)
                    .Topic(pushSettings.APNSTopic)
                    .Environment(pushSettings.APNSEnvironment)
                    .ExecuteAsync()
                    .ConfigureAwait(false))
                .ToChatOperationResult("Chat.RegisterPushChannels()", this);
        }
        
        /// <summary>
        /// Returns the IDs of all currently registered push channels.
        /// </summary>
        public async Task<ChatOperationResult<List<string>>> GetPushChannels()
        {
            var result = new ChatOperationResult<List<string>>("Chat.GetPushChannels()", this);
            var pushSettings = GetCommonPushOptions;
            var audit = await PubnubInstance.AuditPushChannelProvisions()
                .PushType(pushSettings.DeviceGateway)
                .DeviceId(pushSettings.DeviceToken)
                .Topic(pushSettings.APNSTopic)
                .Environment(pushSettings.APNSEnvironment)
                .ExecuteAsync()
                .ConfigureAwait(false);
            if (result.RegisterOperation(audit))
            {
                return result;
            }
            result.Result = audit.Result.Channels;
            return result;
        }
        
        #endregion
        
        /// <summary>
        /// Destroys the chat instance and cleans up resources.
        /// <para>
        /// Stops user activity tracking, destroys the PubNub instance, and disposes the rate limiter.
        /// </para>
        /// </summary>
        public void Destroy()
        {
            storeActivity = false;
            PubnubInstance.Destroy();
            RateLimiter.Dispose();
        }

        ~Chat()
        {
            Destroy();
        }
    }
}