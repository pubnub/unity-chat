using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PubnubApi;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Entities.Events;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    //TODO: make IDisposable?
    //TODO: global remove CCoreException from inline docs
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
        private Dictionary<string, Channel> channelWrappers = new();
        private Dictionary<string, User> userWrappers = new();
        //TODO: wrappers rethink
        internal Dictionary<string, Membership> membershipWrappers = new();
        private Dictionary<string, Message> messageWrappers = new();
        private bool fetchUpdates = true;

        public Pubnub PubnubInstance { get; }
        internal ChatListenerFactory ListenerFactory { get; }

        public event Action<ChatEvent> OnAnyEvent;

        public ChatAccessManager ChatAccessManager { get; }
        public PubnubChatConfig Config { get; }

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
        public Chat(PubnubChatConfig chatConfig, PNConfiguration pubnubConfig, ChatListenerFactory? listenerFactory = null)
        {
            PubnubInstance = new Pubnub(pubnubConfig);
            ListenerFactory = listenerFactory ?? new DotNetListenerFactory();
            Config = chatConfig;
            ChatAccessManager = new ChatAccessManager(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chat"/> class.
        /// <para>
        /// Creates a new chat instance.
        /// </para>
        /// </summary>
        /// <param name="chatConfig">Config with Chat specific parameters</param>
        /// <param name="pubnub">An already initialised instance of Pubnub</param>
        /// /// <param name="listenerFactory">Optional injectable listener factory, used in Unity to allow for dispatching Chat callbacks on main thread.</param>
        /// <remarks>
        /// The constructor initializes the Chat object with the provided existing Pubnub instance.
        /// </remarks>
        public Chat(PubnubChatConfig chatConfig, Pubnub pubnub, ChatListenerFactory? listenerFactory = null)
        {
            Config = chatConfig;
            PubnubInstance = pubnub;
            ListenerFactory = listenerFactory ?? new DotNetListenerFactory();
            ChatAccessManager = new ChatAccessManager(this);
        }
        
        #region Channels

        public void AddListenerToChannelsUpdate(List<string> channelIds, Action<Channel> listener)
        {
            foreach (var channelId in channelIds)
            {
                if (TryGetChannel(channelId, out var channel))
                {
                    channel.OnChannelUpdate += listener;
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
        public async Task<Channel?> CreatePublicConversation(string channelId = "")
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
        public async Task<Channel?> CreatePublicConversation(string channelId, ChatChannelData additionalData)
        {
            var existingChannel = await GetChannelAsync(channelId);
            if (existingChannel != null)
            {
                PubnubInstance.PNConfig.Logger?.Debug("Trying to create a channel with ID that already exists! Returning existing one.");
                return existingChannel;
            }

            additionalData.ChannelType = "public";
            var updated = await Channel.UpdateChannelData(this, channelId, additionalData);
            if (updated)
            {
                var channel = new Channel(this, channelId, additionalData);
                channelWrappers.Add(channelId, channel);
                return channel;
            }
            else
            {
                return null;
            }
        }

        private async Task<CreatedChannelWrapper?> CreateConversation(
            string type, 
            List<User> users, 
            string channelId = "", 
            ChatChannelData? channelData = null, 
            ChatMembershipData? membershipData = null)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                channelId = Guid.NewGuid().ToString();
            }
            
            var existingChannel = await GetChannelAsync(channelId);
            if (existingChannel != null)
            {
                PubnubInstance.PNConfig.Logger?.Warn("Trying to create a channel with ID that already exists! Aborting.");
                return null;
            }
            
            channelData ??= new ChatChannelData();
            channelData.ChannelType = type;
            var updated = await Channel.UpdateChannelData(this, channelId, channelData);
            if (!updated)
            {
                return null;
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
            
            if (setMembershipResult.Status.Error)
            {
                PubnubInstance.PNConfig.Logger?.Error($"Error when trying to set memberships for {type} conversation: {setMembershipResult.Status.Error}");
                return null;
            }

            var responseWrapper = new CreatedChannelWrapper();
            if (membershipWrappers.TryGetValue(currentUserId + channelId, out var existingHostMembership))
            {
                existingHostMembership.UpdateLocalData(membershipData);
                responseWrapper.HostMembership = existingHostMembership;
            }
            else
            {
                var hostMembership = new Membership(this, currentUserId, channelId, membershipData);
                membershipWrappers.Add(hostMembership.Id, hostMembership);
                responseWrapper.HostMembership = hostMembership;
            }
            
            var channel = new Channel(this, channelId, channelData);
            channelWrappers.Add(channelId, channel);

            if (type == "direct")
            {
                var inviteMembership = await InviteToChannel(channelId, users[0].Id);
                if (inviteMembership == null)
                {
                    PubnubInstance.PNConfig.Logger?.Error($"Error when trying to invite user \"{users[0].Id}\" to direct conversation \"{channelId}\": {setMembershipResult.Status.Error}");
                    return null;
                }
                responseWrapper.InviteesMemberships = new List<Membership>() { inviteMembership };
            }else if (type == "group")
            {
                var inviteMembership = await InviteMultipleToChannel(channelId, users);
                if (inviteMembership?.Count == 0)
                {
                    PubnubInstance.PNConfig.Logger?.Error($"Error when trying to invite users to group conversation \"{channelId}\": {setMembershipResult.Status.Error}");
                    return null;
                }
                responseWrapper.InviteesMemberships = new List<Membership>(inviteMembership);
            }
            return responseWrapper;
        }

        public async Task<CreatedChannelWrapper?> CreateDirectConversation(User user, string channelId = "",
            ChatChannelData? channelData = null, ChatMembershipData? membershipData = null)
        {
            return await CreateConversation("direct", new List<User>() { user }, channelId, channelData,
                membershipData);
        }

        public async Task<CreatedChannelWrapper?> CreateGroupConversation(List<User> users, string channelId = "",
            ChatChannelData? channelData = null, ChatMembershipData? membershipData = null)
        {
            return await CreateConversation("group", users, channelId, channelData,
                membershipData);
        }

        public async Task<Membership?> InviteToChannel(string channelId, string userId)
        {
            //Check if already a member first
            var members = await GetChannelMemberships(channelId, filter:$"uuid.id == \"{userId}\"");
            if (members != null && members.Memberships.Any())
            {
                //Already a member, just return current membership
                return members.Memberships[0];
            }
            
            var channel = await GetChannelAsync(channelId);
            if (channel == null)
            {
                PubnubInstance.PNConfig?.Logger.Error($"Error: tried to invite user \"{userId}\" to channel \"{channelId}\" but such channel doesn't exist!");
                return null;
            }

            var response = await PubnubInstance.SetMemberships().Uuid(userId).Include(new[]
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

            if (response.Status.Error)
            {
                PubnubInstance.PNConfig?.Logger.Error($"Error when trying to invite user \"{userId}\" to channel \"{channelId}\": {response.Status.ErrorData.Information}");
                return null;
            }
            
            var newMataData = response.Result.Memberships?.FirstOrDefault(x => x.ChannelMetadata.Channel == channelId)?
                .ChannelMetadata;
            if (newMataData != null)
            {
                channel.UpdateLocalData(newMataData);
            }

            var inviteEventPayload = $"{{\"channelType\": \"{channel.Type}\", \"channelId\": {channelId}}}";
            await EmitEvent(PubnubChatEventType.Invite, userId, inviteEventPayload);
            
            var newMembership = new Membership(this, userId, channelId, new ChatMembershipData());
            await newMembership.SetLastReadMessageTimeToken(ChatUtils.TimeTokenNow());
            membershipWrappers.Add(newMembership.Id, newMembership);

            return newMembership;
        }

        public async Task<List<Membership>> InviteMultipleToChannel(string channelId, List<User> users)
        {
            var memberships = new List<Membership>();
            var channel = await GetChannelAsync(channelId);
            if (channel == null)
            {
                PubnubInstance.PNConfig?.Logger.Error($"Error: tried to invite multiple users to channel \"{channelId}\" but such channel doesn't exist!");
                return memberships;
            }
            var inviteResponse = await PubnubInstance.SetChannelMembers().Channel(channelId)
                .Include(
                    //TODO: C# FIX, MISSING VALUES
                    new[] { 
                        PNChannelMemberField.UUID, 
                        PNChannelMemberField.CUSTOM, 
                        PNChannelMemberField.UUID_CUSTOM
                    })
                //TODO: again, should ChatMembershipData from Create(...)Channel  also be passed here?
                .Uuids(users.Select(x => new PNChannelMember() { Custom = x.CustomData, Uuid = x.Id }).ToList())
                .ExecuteAsync();
            
            if (inviteResponse.Status.Error)
            {
                PubnubInstance.PNConfig?.Logger.Error($"Error when trying to invite multiple users to channel \"{channelId}\": {inviteResponse.Status.ErrorData.Information}");
                return memberships;
            }
            
            var usersDict = users.ToDictionary(x => x.Id, y => y);
            foreach (var channelMember in inviteResponse.Result.ChannelMembers)
            {
                var userId = channelMember.UuidMetadata.Uuid;
                if (membershipWrappers.TryGetValue(userId + channelId,
                        out var existingMembership))
                {
                    usersDict[userId].UpdateLocalData(channelMember.UuidMetadata);
                    existingMembership.UpdateLocalData(channelMember);
                    memberships.Add(existingMembership);
                }
                else
                {
                    var newMembership = new Membership(this, userId, channelId, channelMember);
                    await newMembership.SetLastReadMessageTimeToken(ChatUtils.TimeTokenNow());
                    membershipWrappers.Add(newMembership.Id, newMembership);
                    memberships.Add(newMembership);
                }
                
                var inviteEventPayload = $"{{\"channelType\": \"{channel.Type}\", \"channelId\": {channelId}}}";
                await EmitEvent(PubnubChatEventType.Invite, userId, inviteEventPayload);
            }

            await channel.Resync();

            return memberships;
        }

        /// <summary>
        /// Gets the channel by the provided channel ID.
        /// <para>
        /// Tries to get the channel by the provided channel ID.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="channel">The out channel.</param>
        /// <returns>True if the channel was found, false otherwise.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// if (chat.TryGetChannel("channel_id", out var channel)) {
        ///    // Channel found
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="Channel"/>
        /// <seealso cref="GetChannelAsync"/>
        public bool TryGetChannel(string channelId, out Channel channel)
        {
            channel = GetChannelAsync(channelId).Result;
            return channel != null;
        }

        /// <summary>
        /// Performs an async retrieval of a Channel object with a given ID.
        /// </summary>
        /// <param name="channelId">ID of the channel.</param>
        /// <returns>Channel object if it exists, null otherwise.</returns>
        public async Task<Channel?> GetChannelAsync(string channelId)
        {
            if (channelWrappers.TryGetValue(channelId, out var existingChannel))
            {
                await existingChannel.Resync();
                return existingChannel;
            }
            else
            {
                var data = await Channel.GetChannelData(this, channelId);
                if (data == null)
                {
                    return null;
                }
                else
                {
                    var channel = new Channel(this, channelId, data);
                    channelWrappers.Add(channelId, channel);
                    return channel;
                }
            }
        }

        public async Task<ChannelsResponseWrapper> GetChannels(string filter = "", string sort = "", int limit = 0,
            Page page = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the channel with the provided channel ID.
        /// <para>
        /// Updates the channel with the provided channel ID with the provided data.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="updatedData">The updated data for the channel.</param>
        /// <exception cref="PubNubCCoreException"> Throws an exception if the channel with the provided ID does not exist or any connection problem persists.</exception>
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
        public async Task UpdateChannel(string channelId, ChatChannelData updatedData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes the channel with the provided channel ID.
        /// <para>
        /// The channel is deleted with all the messages and users.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <exception cref="PubNubCCoreException"> Throws an exception if the channel with the provided ID does not exist or any connection problem persists.</exception>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// chat.DeleteChannel("channel_id");
        /// </code>
        /// </example>
        public async Task DeleteChannel(string channelId)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Users

        public async Task<UserMentionsWrapper> GetCurrentUserMentions(string startTimeToken, string endTimeToken,
            int count)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Tries to retrieve the current User object for this chat.
        /// </summary>
        /// <param name="user">The retrieved current User object.</param>
        /// <returns>True if chat has a current user, false otherwise.</returns>
        /// <seealso cref="GetCurrentUserAsync"/>
        public bool TryGetCurrentUser(out User user)
        {
            user = GetCurrentUserAsync().Result;
            return user != null;
        }

        /// <summary>
        /// Asynchronously tries to retrieve the current User object for this chat.
        /// </summary>
        /// <returns>User object if there is a current user, null otherwise.</returns>
        public async Task<User?> GetCurrentUserAsync()
        {
            var userId = PubnubInstance.GetCurrentUserId();
            return await GetUserAsync(userId);
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
        /// <exception cref="PubNubCCoreException"> Throws an exception if the user with the provided ID does not exist or any connection problem persists.</exception>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// chat.SetRestrictions("user_id", "channel_id", true, true, "Spamming");
        /// </code>
        /// </example>
        public async Task SetRestriction(string userId, string channelId, bool banUser, bool muteUser, string reason)
        {
            throw new NotImplementedException();
        }

        public async Task SetRestriction(string userId, string channelId, Restriction restriction)
        {
            await SetRestriction(userId, channelId, restriction.Ban, restriction.Mute, restriction.Reason);
        }

        public void AddListenerToUsersUpdate(List<string> userIds, Action<User> listener)
        {
            foreach (var userId in userIds)
            {
                if (TryGetUser(userId, out var user))
                {
                    user.OnUserUpdated += listener;
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
        /// <exception cref="PubNubCCoreException"> Throws an exception if any connection problem persists.</exception>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var user = chat.CreateUser("user_id");
        /// </code>
        /// </example>
        /// <seealso cref="User"/>
        public async Task<User?> CreateUser(string userId)
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
        /// <exception cref="PubNubCCoreException"> Throws an exception if any connection problem persists.</exception>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// var user = chat.CreateUser("user_id");
        /// </code>
        /// </example>
        /// <seealso cref="User"/>
        public async Task<User?> CreateUser(string userId, ChatUserData additionalData)
        {
            var existingUser = await GetUserAsync(userId);
            if (existingUser != null)
            {
                Debug.WriteLine("Trying to create a user with ID that already exists! Returning existing one.");
                return existingUser;
            }
            
            var updated = await User.UpdateUserData(this, userId, additionalData);
            if (updated)
            {
                var user = new User(this, userId, additionalData);
                userWrappers.Add(userId, user);
                return user;
            }
            else
            {
                return null;
            }
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
        /// <exception cref="PubNubCCoreException"> Throws an exception if any connection problem persists.</exception>
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
        public async Task<bool> IsPresent(string userId, string channelId)
        {
            if (TryGetChannel(channelId, out var channel))
            {
                return await channel.IsUserPresent(userId);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the list of users present in the provided channel.
        /// <para>
        /// Gets all the users as a list of the strings present in the provided channel.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <returns>The list of the users present in the channel.</returns>
        /// <exception cref="PubNubCCoreException"> Throws an exception if any connection problem persists.</exception>
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
        public async Task<List<string>> WhoIsPresent(string channelId)
        {
            if (TryGetChannel(channelId, out var channel))
            {
                return await channel.WhoIsPresent();
            }
            else
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets the list of channels where the user with the provided user ID is present.
        /// <para>
        /// Gets all the channels as a list of the strings where the user with the provided user ID is present.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The list of the channels where the user is present.</returns>
        /// <exception cref="PubNubCCoreException"> Throws an exception if any connection problem persists.</exception>
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
        public async Task<List<string>> WherePresent(string userId)
        {
            if (TryGetUser(userId, out var user))
            {
                return await user.WherePresent();
            }
            else
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets the user with the provided user ID.
        /// <para>
        /// Tries to get the user with the provided user ID.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="user">The out user.</param>
        /// <returns>True if the user was found, false otherwise.</returns>
        /// <exception cref="PubNubCCoreException"> Throws an exception if any connection problem persists.</exception>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// if (chat.TryGetUser("user_id", out var user)) {
        ///   // User found
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="User"/>
        /// <seealso cref="GetUserAsync"/>
        public bool TryGetUser(string userId, out User user)
        {
            user = GetUserAsync(userId).Result;
            return user != null;
        }

        /// <summary>
        /// Asynchronously gets the user with the provided user ID.
        /// </summary>
        /// <param name="userId">ID of the User to get.</param>
        /// <returns>User object if one with given ID is found, null otherwise.</returns>
        public async Task<User?> GetUserAsync(string userId)
        {
            if (userWrappers.TryGetValue(userId, out var existingUser))
            {
                await existingUser.Resync();
                return existingUser;
            }
            else
            {
                var data = await User.GetUserData(this, userId);
                if (data == null)
                {
                    return null;
                }
                else
                {
                    var user = new User(this, userId, data);
                    userWrappers.Add(userId, user);
                    return user;
                }
            }
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
        /// <exception cref="PubNubCCoreException"> Throws an exception if any connection problem persists.</exception>
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
            var result = await PubnubInstance.GetAllUuidMetadata().Filter(filter).Sort(new List<string>() { sort })
                .Limit(limit).Page(page).ExecuteAsync();
            if (result.Status.Error)
            {
                PubnubInstance.PNConfig.Logger?.Error($"Error when trying to GetUsers(): {result.Status.ErrorData.Information}");
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
                if (userWrappers.TryGetValue(resultMetadata.Uuid, out var existingUserWrapper))
                {
                    existingUserWrapper.UpdateLocalData(resultMetadata);
                    response.Users.Add(existingUserWrapper);
                }
                else
                {
                    var user = new User(this, resultMetadata.Uuid, resultMetadata);
                    userWrappers.Add(user.Id, user);
                    response.Users.Add(user);
                }
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
        /// <exception cref="PubNubCCoreException"> Throws an exception if the user with the provided ID does not exist or any connection problem persists.</exception>
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
            if (userWrappers.TryGetValue(userId, out var existingUserWrapper))
            {
                await existingUserWrapper.Update(updatedData);
            }
            else
            {
                await User.UpdateUserData(this, userId, updatedData);
            }
        }

        /// <summary>
        /// Deletes the user with the provided user ID.
        /// <para>
        /// The user is deleted with all the messages and channels.
        /// </para>
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <exception cref="PubNubCCoreException"> Throws an exception if the user with the provided ID does not exist or any connection problem persists.</exception>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// chat.DeleteUser("user_id");
        /// </code>
        /// </example>
        public async Task DeleteUser(string userId)
        {
            throw new NotImplementedException();
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
        /// <exception cref="PubNubCCoreException"> Throws an exception if the user with the provided ID does not exist or any connection problem persists.</exception>
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
        public async Task<MembersResponseWrapper> GetUserMemberships(string userId, string filter = "",
            string sort = "",
            int limit = 0, Page page = null)
        {
            throw new NotImplementedException();
        }

        public void AddListenerToMembershipsUpdate(List<string> membershipIds, Action<Membership> listener)
        {
            foreach (var membershipId in membershipIds)
            {
                if (membershipWrappers.TryGetValue(membershipId, out var membership))
                {
                    membership.OnMembershipUpdated += listener;
                }
            }
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
        /// <exception cref="PubNubCCoreException"> Throws an exception if the channel with the provided ID does not exist or any connection problem persists.</exception>
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
        public async Task<MembersResponseWrapper?> GetChannelMemberships(string channelId, string filter = "",
            string sort = "",
            int limit = 0, PNPageObject page = null)
        {
            var result = await PubnubInstance.GetChannelMembers().Include(
                    new[]
                    {
                        //TODO: C# FIX
                        //PNChannelMemberField.CHANNEL_CUSTOM,
                        PNChannelMemberField.CUSTOM,
                        //PNChannelMemberField.CHANNEL,
                        //PNChannelMemberField.STATUS,
                    }).Channel(channelId).Filter(filter).Sort(new List<string>() { sort })
                .Limit(limit).Page(page).ExecuteAsync();
            if (result.Status.Error)
            {
                PubnubInstance.PNConfig.Logger?.Error($"Error when trying to get \"{channelId}\" channel members: {result.Status.ErrorData.Information}");
                return null;
            }

            var memberships = new List<Membership>();
            foreach (var channelMemberResult in result.Result.ChannelMembers)
            {
                var membershipId = channelMemberResult.UuidMetadata.Uuid + channelId;
                if (membershipWrappers.TryGetValue(membershipId, out var existingMembershipWrapper))
                {
                    existingMembershipWrapper.MembershipData.CustomData = channelMemberResult.Custom;
                    memberships.Add(existingMembershipWrapper);
                }
                else
                {
                    memberships.Add(new Membership(this, channelMemberResult.UuidMetadata.Uuid, channelId, new ChatMembershipData()
                    {
                        CustomData = channelMemberResult.Custom
                    }));
                }
            }
            return new MembersResponseWrapper()
            {
                Memberships = memberships,
                Page = new Page()
                {
                    Next = result.Result.Page.Next,
                    Previous = result.Result.Page.Prev
                },
                Total = result.Result.TotalCount
            };
        }

        #endregion

        #region Messages

        //TODO: wrappers rethink
        internal void RegisterMessage(Message message)
        {
            messageWrappers.TryAdd(message.Id, message);
        }
        
        public async Task<EventsHistoryWrapper> GetMessageReportsHistory(string channelId, string startTimeToken,
            string endTimeToken, int count)
        {
            return await GetEventsHistory($"PUBNUB_INTERNAL_MODERATION_{channelId}", startTimeToken, endTimeToken,
                count);
        }

        /// <summary>
        /// Gets the <c>Message</c> object for the given timetoken.
        /// <para>
        /// Gets the <c>Message</c> object from the channel for the given timetoken.
        /// The timetoken is used to identify the message.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="messageTimeToken">The timetoken of the message.</param>
        /// <param name="message">The out parameter that contains the <c>Message</c> object.</param>
        /// <returns><c>true</c> if the message is found; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// if (chat.TryGetMessage("channel_id", "timetoken", out var message)) {
        ///  // Message found
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="Message"/>
        /// <seealso cref="GetMessageAsync"/>
        public bool TryGetMessage(string channelId, string messageTimeToken, out Message message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously gets the <c>Message</c> object for the given timetoken.
        /// </summary>
        /// <param name="channelId">ID of the channel on which the message was sent.</param>
        /// <param name="messageTimeToken">TimeToken of the searched-for message.</param>
        /// <returns>Message object if one was found, null otherwise.</returns>
        public async Task<Message?> GetMessageAsync(string channelId, string messageTimeToken)
        {
            return await Task.Run(() =>
            {
                var result = TryGetMessage(channelId, messageTimeToken, out var message);
                return result ? message : null;
            });
        }

        public async Task<MarkMessagesAsReadWrapper> MarkAllMessagesAsRead(string filter = "", string sort = "",
            int limit = 0,
            Page page = null)
        {
            throw new NotImplementedException();
        }

        internal bool TryGetAnyMessage(string timeToken, out Message message)
        {
            return messageWrappers.TryGetValue(timeToken, out message);
        }

        public async Task<List<UnreadMessageWrapper>> GetUnreadMessagesCounts(string filter = "", string sort = "",
            int limit = 0,
            Page page = null)
        {
            throw new NotImplementedException();
        }

        public async Task<ThreadChannel> CreateThreadChannel(Message message)
        {
            throw new NotImplementedException();
        }

        public async Task RemoveThreadChannel(Message message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tries to retrieve a ThreadChannel object from a Message object if there is one.
        /// </summary>
        /// <param name="message">Message on which the ThreadChannel is supposed to be.</param>
        /// <param name="threadChannel">Retrieved ThreadChannel or null if it wasn't found/</param>
        /// <returns>True if a ThreadChannel was found, false otherwise.</returns>
        /// <seealso cref="GetThreadChannelAsync"/>
        public bool TryGetThreadChannel(Message message, out ThreadChannel threadChannel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously tries to retrieve a ThreadChannel object from a Message object if there is one.
        /// </summary>
        /// <param name="message">Message on which the ThreadChannel is supposed to be.</param>
        /// <returns>The ThreadChannel object if one was found, null otherwise.</returns>
        public async Task<ThreadChannel?> GetThreadChannelAsync(Message message)
        {
            return await Task.Run(() =>
            {
                var result = TryGetThreadChannel(message, out var threadChannel);
                return result ? threadChannel : null;
            });
        }

        public async Task ForwardMessage(Message message, Channel channel)
        {
            throw new NotImplementedException();
        }

        public void AddListenerToMessagesUpdate(string channelId, List<string> messageTimeTokens,
            Action<Message> listener)
        {
            foreach (var messageTimeToken in messageTimeTokens)
            {
                if (TryGetMessage(channelId, messageTimeToken, out var message))
                {
                    message.OnMessageUpdated += listener;
                }
            }
        }

        public async Task PinMessageToChannel(string channelId, Message message)
        {
            if (TryGetChannel(channelId, out var channel))
            {
                await channel.PinMessage(message);
            }
        }

        public async Task UnpinMessageFromChannel(string channelId)
        {
            if (TryGetChannel(channelId, out var channel))
            {
                await channel.UnpinMessage();
            }
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
        /// <exception cref="PubNubCCoreException"> Throws an exception if the channel with the provided ID does not exist or any connection problem persists.</exception>
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
        public async Task<List<Message>> GetChannelMessageHistory(string channelId, string startTimeToken,
            string endTimeToken,
            int count)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Events

        public async Task<EventsHistoryWrapper> GetEventsHistory(string channelId, string startTimeToken,
            string endTimeToken,
            int count)
        {
            throw new NotImplementedException();
        }
        
        public async Task EmitEvent(PubnubChatEventType type, string channelId, string jsonPayload)
        {
            jsonPayload = jsonPayload.Remove(0, 1);
            jsonPayload = jsonPayload.Remove(jsonPayload.Length - 1);
            var fullPayload = $"{{{jsonPayload}, \"type\": {ChatEnumConverters.ChatEventTypeToString(type)}}}";
            await PubnubInstance.Publish().Channel(channelId).Message(fullPayload).ExecuteAsync();
        }

        #endregion

        public void Destroy()
        {
            PubnubInstance.Destroy();
        }

        ~Chat()
        {
            Destroy();
        }
    }
}