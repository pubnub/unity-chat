using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;

namespace PubnubChatApi
{
    /// <summary>
    /// Represents a user in the chat. 
    /// <para>
    /// You can get information about the user, update the user's data, delete the user, set restrictions on the user,
    /// </para>
    /// </summary>
    public class User : UniqueChatEntity
    {
        private ChatUserData userData;

        /// <summary>
        /// The user's user name. 
        /// <para>
        /// This might be user's display name in the chat.
        /// </para>
        /// </summary>
        public string UserName => userData.Username;

        /// <summary>
        /// The user's external id.
        /// <para>
        /// This might be user's id in the external system (e.g. Database, CRM, etc.)
        /// </para>
        /// </summary>
        public string ExternalId => userData.ExternalId;

        /// <summary>
        /// The user's profile url.
        /// <para>
        /// This might be user's profile url to download the profile picture.
        /// </para>
        /// </summary>
        public string ProfileUrl => userData.ProfileUrl;

        /// <summary>
        /// The user's email.
        /// <para>
        /// This should be user's email address.
        /// </para>
        /// </summary>
        public string Email => userData.Email;

        /// <summary>
        /// The user's custom data.
        /// <para>
        /// This might be any custom data that you want to store for the user.
        /// </para>
        /// </summary>
        public Dictionary<string, object> CustomData => userData.CustomData;

        /// <summary>
        /// The user's status.
        /// <para>
        /// This is a string that represents the user's status.
        /// </para>
        /// </summary>
        public string Status => userData.Status;

        /// <summary>
        /// The user's data type.
        /// <para>
        /// This is a string that represents the user's data type.
        /// </para>
        /// </summary>
        public string DataType => userData.Type;

        public bool Active
        {
            get
            {
                if (CustomData == null || !CustomData.TryGetValue("lastActiveTimestamp", out var lastActiveTimestamp))
                {
                    return false;
                }
                var currentTimeStamp = ChatUtils.TimeTokenNowLong();
                var interval = chat.Config.StoreUserActivityInterval;
                var lastActive = Convert.ToInt64(lastActiveTimestamp);
                return currentTimeStamp - lastActive <= interval * 1000000;
            }
        }

        public string LastActiveTimeStamp
        {
            get
            {
                if (CustomData == null || !CustomData.TryGetValue("lastActiveTimestamp", out var lastActiveTimestamp))
                {
                    return string.Empty;
                }
                return lastActiveTimestamp.ToString();
            }
        }
        
        /// <summary>
        /// Returns true if the User has been soft-deleted.
        /// </summary>
        public bool IsDeleted
        {
            get
            {
                if (CustomData == null || !CustomData.TryGetValue("deleted", out var deletedValue))
                {
                    return false;
                }
                return (bool)deletedValue;
            }
        }
        
        /// <summary>
        /// Event that is triggered when the user is updated.
        /// <para>
        /// This event is triggered when the user's data is updated.
        /// You can subscribe to this event to get notified when the user is updated.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// // var user = // ...;
        /// user.OnUserUpdated += (user) =>
        /// {
        ///    Console.WriteLine($"User {user.UserName} is updated.");
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="Update"/>
        /// <seealso cref="User"/>
        public event Action<User> OnUserUpdated;

        private Subscription mentionsSubscription;
        private Subscription invitesSubscription;
        private Subscription moderationSubscription;
        public event Action<ChatEvent> OnMentionEvent;
        public event Action<ChatEvent> OnInviteEvent;
        public event Action<ChatEvent> OnModerationEvent;

        protected override string UpdateChannelId => Id;

        internal User(Chat chat, string userId, ChatUserData chatUserData) : base(chat, userId)
        {
            UpdateLocalData(chatUserData);
        }
        
        protected override SubscribeCallback CreateUpdateListener()
        {
            return chat.ListenerFactory.ProduceListener(objectEventCallback: delegate(Pubnub pn, PNObjectEventResult e)
            {
                if (ChatParsers.TryParseUserUpdate(chat, this, e, out var updatedData))
                {
                    UpdateLocalData(updatedData);
                    OnUserUpdated?.Invoke(this);
                }
            });
        }
        
        /// <summary>
        /// Sets whether to listen for mention events for this user.
        /// <para>
        /// When enabled, the user will receive mention events when they are mentioned in messages.
        /// </para>
        /// </summary>
        /// <param name="listen">True to start listening, false to stop listening.</param>
        /// <seealso cref="OnMentionEvent"/>
        public void SetListeningForMentionEvents(bool listen)
        {
            SetListening(ref mentionsSubscription, SubscriptionOptions.None, listen, Id, chat.ListenerFactory.ProduceListener(messageCallback:
                delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseEvent(chat, m, PubnubChatEventType.Mention, out var mentionEvent))
                    {
                        OnMentionEvent?.Invoke(mentionEvent);
                        chat.BroadcastAnyEvent(mentionEvent);
                    }
                }));
        }

        /// <summary>
        /// Sets whether to listen for invite events for this user.
        /// <para>
        /// When enabled, the user will receive invite events when they are invited to channels.
        /// </para>
        /// </summary>
        /// <param name="listen">True to start listening, false to stop listening.</param>
        /// <seealso cref="OnInviteEvent"/>
        public void SetListeningForInviteEvents(bool listen)
        {
            SetListening(ref invitesSubscription, SubscriptionOptions.None, listen, Id, chat.ListenerFactory.ProduceListener(messageCallback:
                delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseEvent(chat, m, PubnubChatEventType.Invite, out var inviteEvent))
                    {
                        OnInviteEvent?.Invoke(inviteEvent);
                        chat.BroadcastAnyEvent(inviteEvent);
                    }
                }));
        }

        /// <summary>
        /// Sets whether to listen for moderation events for this user.
        /// <para>
        /// When enabled, the user will receive moderation events such as bans, mutes, and other restrictions.
        /// </para>
        /// </summary>
        /// <param name="listen">True to start listening, false to stop listening.</param>
        /// <seealso cref="OnModerationEvent"/>
        public void SetListeningForModerationEvents(bool listen)
        {
            SetListening(ref moderationSubscription, SubscriptionOptions.None, listen, Chat.INTERNAL_MODERATION_PREFIX+Id, chat.ListenerFactory.ProduceListener(messageCallback:
                delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseEvent(chat, m, PubnubChatEventType.Moderation, out var moderationEvent))
                    {
                        OnModerationEvent?.Invoke(moderationEvent);
                        chat.BroadcastAnyEvent(moderationEvent);
                    }
                }));
        }
        
        /// <summary>
        /// Updates the user.
        /// <para>
        /// This method updates the user's data.
        /// </para>
        /// </summary>
        /// <param name="updatedData">The updated data for the user.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// var result = await user.Update(new ChatUserData
        /// {
        ///    UserName = "New User Name",
        /// });
        /// </code>
        /// </example>
        /// <seealso cref="ChatUserData"/>
        public async Task<ChatOperationResult> Update(ChatUserData updatedData)
        {
            UpdateLocalData(updatedData);
            var result = new ChatOperationResult("User.Update()", chat);
            result.RegisterOperation(await UpdateUserData(chat, Id, updatedData).ConfigureAwait(false));
            return result;
        }

        internal static async Task<PNResult<PNSetUuidMetadataResult>> UpdateUserData(Chat chat, string userId, ChatUserData chatUserData)
        {
            var operation = chat.PubnubInstance.SetUuidMetadata().IncludeCustom(true).IncludeStatus(true).IncludeType(true).Uuid(userId);
            if (!string.IsNullOrEmpty(chatUserData.Username))
            {
                operation = operation.Name(chatUserData.Username);
            }
            if (!string.IsNullOrEmpty(chatUserData.Email))
            {
                operation = operation.Email(chatUserData.Email);
            }
            if (!string.IsNullOrEmpty(chatUserData.ExternalId))
            {
                operation = operation.ExternalId(chatUserData.ExternalId);
            }
            if (!string.IsNullOrEmpty(chatUserData.ProfileUrl))
            {
                operation = operation.ProfileUrl(chatUserData.ProfileUrl);
            }
            if (!string.IsNullOrEmpty(chatUserData.Type))
            {
                operation = operation.Type(chatUserData.Type);
            }
            if (!string.IsNullOrEmpty(chatUserData.Status))
            {
                operation = operation.Status(chatUserData.Status);
            }
            if (chatUserData.CustomData != null)
            {
                operation = operation.Custom(chatUserData.CustomData);
            }
            return await operation.ExecuteAsync().ConfigureAwait(false);
        }
        
        internal static async Task<PNResult<PNGetUuidMetadataResult>> GetUserData(Chat chat, string userId)
        {
            return await chat.PubnubInstance.GetUuidMetadata().Uuid(userId).IncludeCustom(true).ExecuteAsync().ConfigureAwait(false);
        }

        internal void UpdateLocalData(ChatUserData? newData)
        {
            if (newData == null)
            {
                return;
            }
            userData = newData;
        }
        
        /// <summary>
        /// Refreshes the user data from the server.
        /// <para>
        /// Fetches the latest user information from the server and updates the local data.
        /// This is useful when you want to ensure you have the most up-to-date user information.
        /// </para>
        /// </summary>
        /// <returns>A ChatOperationResult indicating the success or failure of the refresh operation.</returns>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// var result = await user.Refresh();
        /// if (!result.Error) {
        ///     // User data has been refreshed
        ///     Console.WriteLine($"User name: {user.UserName}");
        /// }
        /// </code>
        /// </example>
        public override async Task<ChatOperationResult> Refresh()
        {
            var result = new ChatOperationResult("User.Refresh()", chat);
            var getUserData = await GetUserData(chat, Id).ConfigureAwait(false);
            if (result.RegisterOperation(getUserData))
            {
                return result;
            }
            UpdateLocalData(getUserData.Result);
            return result;
        }

        /// <summary>
        /// Deletes the user.
        /// <para>
        /// This method deletes the user from the chat.
        /// It will remove the user from all the channels and delete the user's data.
        /// </para>
        /// </summary>
        /// <param name="soft">Whether to perform a soft delete (true) or hard delete (false).</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// await user.Delete();
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> Delete(bool soft)
        {
            var result = new ChatOperationResult("User.Delete()", chat);
            if (!soft)
            {
                var hardDeleteResult = await chat.DeleteUser(Id).ConfigureAwait(false);
                result.RegisterOperation(hardDeleteResult);
            }
            else
            {
                userData.CustomData ??= new Dictionary<string, object>();
                userData.CustomData["deleted"] = true;
                var updateResult =  await UpdateUserData(chat, Id, userData).ConfigureAwait(false);
                result.RegisterOperation(updateResult);
            }
            return result;
        }
        
        /// <summary>
        /// Restores a previously deleted user.
        /// <para>
        /// Undoes the soft deletion of this user.
        /// This only works for users that were soft deleted.
        /// </para>
        /// </summary>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// if (user.IsDeleted) {
        ///     var result = await user.Restore();
        ///     if (!result.Error) {
        ///         // User has been restored
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="Delete"/>
        /// <seealso cref="IsDeleted"/>
        public async Task<ChatOperationResult> Restore()
        {
            var result = new ChatOperationResult("User.Restore()", chat);
            if (!IsDeleted)
            {
                result.Error = true;
                result.Exception = new PNException("Can't restore a user that wasn't deleted!");
                return result;
            }
            userData.CustomData.Remove("deleted");
            result.RegisterOperation(await UpdateUserData(chat, Id, userData).ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Sets restrictions on the user.
        /// <para>
        /// This method sets the restrictions on the user.
        /// You can ban the user from a channel, mute the user on the channel, or set the restrictions on the user.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel id on which the restrictions are set.</param>
        /// <param name="banUser">If set to <c>true</c>, the user is banned from the channel.</param>
        /// <param name="muteUser">If set to <c>true</c>, the user is muted on the channel.</param>
        /// <param name="reason">The reason for setting the restrictions on the user.</param>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// user.SetRestrictions("channel_id", true, false, "Banned from the channel");
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> SetRestriction(string channelId, bool banUser, bool muteUser, string reason)
        {
            return await chat.SetRestriction(Id, channelId, banUser, muteUser, reason).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets restrictions on the user using a Restriction object.
        /// <para>
        /// This method sets the restrictions on the user using a structured Restriction object
        /// that contains ban, mute, and reason information.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel id on which the restrictions are set.</param>
        /// <param name="restriction">The restriction object containing ban, mute, and reason information.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <seealso cref="SetRestriction(string, bool, bool, string)"/>
        public async Task<ChatOperationResult> SetRestriction(string channelId, Restriction restriction)
        {
            return await chat.SetRestriction(Id, channelId, restriction).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the restrictions on the user for a specific channel.
        /// <para>
        /// This method gets the restrictions (bans and mutes) that have been applied to this user
        /// on the specified channel.
        /// </para>
        /// </summary>
        /// <param name="channel">The channel for which the restrictions are to be fetched.</param>
        /// <returns>A ChatOperationResult containing the Restriction object if restrictions exist for this user on the channel, error otherwise.</returns>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// var channel = // ...;
        /// var result = await user.GetChannelRestrictions(channel);
        /// var restriction = result.Result;
        /// </code>
        /// </example>
        /// <seealso cref="SetRestriction"/>
        public async Task<ChatOperationResult<Restriction>> GetChannelRestrictions(Channel channel)
        {
            var result = new ChatOperationResult<Restriction>("User.GetChannelRestrictions()", chat);
            var membersResult = await chat.PubnubInstance.GetChannelMembers().Channel($"{Chat.INTERNAL_MODERATION_PREFIX}_{channel.Id}").Include(new[]
            {
                PNChannelMemberField.CUSTOM
            }).Filter($"uuid.id == \"{Id}\"").IncludeCount(true).ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(membersResult) || membersResult.Result.ChannelMembers == null || !membersResult.Result.ChannelMembers.Any())
            {
                result.Error = true;
                return result;
            }
            var member = membersResult.Result.ChannelMembers[0];
            try
            {
                result.Result = new Restriction()
                {
                    Ban = (bool)member.Custom["ban"],
                    Mute = (bool)member.Custom["mute"],
                    Reason = (string)member.Custom["reason"]
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
        /// Gets all channel restrictions for this user across all channels.
        /// <para>
        /// This method retrieves all restrictions (bans and mutes) that have been applied to this user
        /// across all channels where they have restrictions.
        /// </para>
        /// </summary>
        /// <param name="sort">Sort criteria for restrictions.</param>
        /// <param name="limit">The maximum number of restrictions to retrieve.</param>
        /// <param name="page">Pagination object for retrieving specific page results.</param>
        /// <returns>A ChatOperationResult containing the wrapper with all channel restrictions for this user.</returns>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// var result = await user.GetChannelsRestrictions(limit: 10);
        /// var restrictions = result.Result.Restrictions;
        /// foreach (var restriction in restrictions) {
        ///     Console.WriteLine($"Channel: {restriction.ChannelId}, Ban: {restriction.Ban}, Mute: {restriction.Mute}");
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="GetChannelRestrictions"/>
        /// <seealso cref="SetRestriction"/>
        public async Task<ChatOperationResult<ChannelsRestrictionsWrapper>> GetChannelsRestrictions(string sort = "", int limit = 0,
            PNPageObject page = null)
        {
            var result = new ChatOperationResult<ChannelsRestrictionsWrapper>("User.GetChannelsRestrictions()", chat){Result = new ChannelsRestrictionsWrapper()};
            var operation = chat.PubnubInstance.GetMemberships().Uuid(Id)
                .Include(new[]
                {
                    PNMembershipField.CUSTOM,
                    PNMembershipField.CHANNEL
                }).Filter($"channel.id LIKE \"{Chat.INTERNAL_MODERATION_PREFIX}_*\"").IncludeCount(true);
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
            var membershipsResult = await operation.ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(membershipsResult))
            {
                return result;
            }

            result.Result.Page = membershipsResult.Result.Page;
            result.Result.Total = membershipsResult.Result.TotalCount;
            foreach (var membership in membershipsResult.Result.Memberships)
            {
                try
                {
                    var internalChannelId = membership.ChannelMetadata.Channel;
                    var removeString = $"{Chat.INTERNAL_MODERATION_PREFIX}_";
                    var index = internalChannelId.IndexOf(removeString, StringComparison.Ordinal);
                    var channelId = (index < 0)
                        ? internalChannelId
                        : internalChannelId.Remove(index, removeString.Length);
                    result.Result.Restrictions.Add(new ChannelRestriction()
                    {
                        Ban = (bool)membership.Custom["ban"],
                        Mute = (bool)membership.Custom["mute"],
                        Reason = (string)membership.Custom["reason"],
                        ChannelId = channelId
                    });
                }
                catch (Exception e)
                {
                    chat.Logger.Warn($"Incorrect data was encountered when parsing Channel Restriction for User \"{Id}\" in Channel \"{membership.ChannelMetadata.Channel}\". Exception was: {e.Message}");
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if the user is present on the channel.
        /// <para>
        /// This method checks if the user is present on the channel.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel id on which the user's presence is to be checked.</param>
        /// <returns>
        /// A ChatOperationResult with <c>true</c> if the user is present on the channel; otherwise, <c>false</c>.
        /// </returns>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// if (user.IsPresentOn("channel_id")) {
        ///   // User is present on the channel
        /// }
        /// </code>
        /// </example>
        public async Task<ChatOperationResult<bool>> IsPresentOn(string channelId)
        {
            var result = new ChatOperationResult<bool>("User.IsPresentOn()", chat);
            var response = await chat.PubnubInstance.WhereNow().Uuid(Id).ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(response))
            {
                return result;
            }
            result.Result = response.Result.Channels.Contains(channelId);
            return result;
        }

        /// <summary>
        /// Gets the list of channels where the user is present.
        /// <para>
        /// This method gets the list of channels where the user is present.
        /// </para>
        /// </summary>
        /// <returns>
        /// A ChatOperationResult containing the list of channels where the user is present.
        /// </returns>
        /// <remarks>
        /// The list is kept as a list of channel ids.
        /// </remarks>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// var result = await user.WherePresent();
        /// var channels = result.Result;
        /// foreach (var channel in channels) {
        ///  Console.WriteLine(channel);
        /// }
        /// </code>
        /// </example>
        public async Task<ChatOperationResult<List<string>>> WherePresent()
        {
            var result = new ChatOperationResult<List<string>>("User.WherePresent()", chat);
            var where = await chat.PubnubInstance.WhereNow().Uuid(Id).ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(where))
            {
                return result;
            }
            result.Result = new List<string>();
            if (where.Result != null)
            {
                result.Result.AddRange(where.Result.Channels);
            }
            return result;
        }

        /// <summary>
        /// Gets the list of memberships of the user.
        /// <para>
        /// This methods gets the list of memberships of the user.
        /// All the relationships of the user with the channels are considered as memberships.
        /// </para>
        /// </summary>
        /// <param name="filter">The filter parameter.</param>
        /// <param name="sort">The sort parameter.</param>
        /// <param name="limit">The limit on the number of memberships to be fetched.</param>
        /// <param name="page">The page object for pagination.</param>
        /// <returns>
        /// A ChatOperationResult containing the list of memberships of the user.
        /// </returns>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// var result = await user.GetMemberships(limit: 50);
        /// var memberships = result.Result.Memberships;
        /// foreach (var membership in memberships) {
        /// Console.WriteLine(membership.ChannelId);
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="Membership"/>
        public async Task<ChatOperationResult<MembersResponseWrapper>> GetMemberships(string filter = "", string sort = "", int limit = 0,
            PNPageObject page = null)
        {
            return await chat.GetUserMemberships(Id, filter, sort, limit, page).ConfigureAwait(false);
        }
    }
}