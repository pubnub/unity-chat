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
using PubnubChatApi.Entities.Events;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
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
                throw new NotImplementedException();
            }
        }

        public string LastActiveTimeStamp
        {
            get
            {
                throw new NotImplementedException();
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
        
        public void SetListeningForMentionEvents(bool listen)
        {
            SetListening(mentionsSubscription, listen, Id, chat.ListenerFactory.ProduceListener(messageCallback:
                delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseEvent(chat, m, PubnubChatEventType.Mention, out var mentionEvent))
                    {
                        OnMentionEvent?.Invoke(mentionEvent);
                        chat.BroadcastAnyEvent(mentionEvent);
                    }
                }));
        }

        public void SetListeningForInviteEvents(bool listen)
        {
            SetListening(invitesSubscription, listen, Id, chat.ListenerFactory.ProduceListener(messageCallback:
                delegate(Pubnub pn, PNMessageResult<object> m)
                {
                    if (ChatParsers.TryParseEvent(chat, m, PubnubChatEventType.Invite, out var inviteEvent))
                    {
                        OnInviteEvent?.Invoke(inviteEvent);
                        chat.BroadcastAnyEvent(inviteEvent);
                    }
                }));
        }

        public void SetListeningForModerationEvents(bool listen)
        {
            SetListening(moderationSubscription, listen, Chat.INTERNAL_MODERATION_PREFIX+Id, chat.ListenerFactory.ProduceListener(messageCallback:
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
        /// <exception cref="PubNubCCoreException">
        /// This exception might be thrown when any error occurs while updating the user.
        /// </exception>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// user.UpdateUser(new ChatUserData
        /// {
        ///    UserName = "New User Name",
        /// });
        /// </code>
        /// </example>
        /// <seealso cref="ChatUserData"/>
        public async Task Update(ChatUserData updatedData)
        {
            UpdateLocalData(updatedData);
            await UpdateUserData(chat, Id, updatedData);
        }

        internal static async Task<PNResult<PNSetUuidMetadataResult>> UpdateUserData(Chat chat, string userId, ChatUserData chatUserData)
        {
            //TODO: Create a better way to do this
            var operation = chat.PubnubInstance.SetUuidMetadata().IncludeCustom(true).Uuid(userId);
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
            if (chatUserData.CustomData.Any())
            {
                operation = operation.Custom(chatUserData.CustomData);
            }
            return await operation.ExecuteAsync();
        }
        
        internal static async Task<PNResult<PNGetUuidMetadataResult>> GetUserData(Chat chat, string userId)
        {
            return await chat.PubnubInstance.GetUuidMetadata().Uuid(userId).IncludeCustom(true).ExecuteAsync();
        }

        internal void UpdateLocalData(ChatUserData? newData)
        {
            if (newData == null)
            {
                return;
            }
            userData = newData;
        }
        
        public override async Task Resync()
        {
            var newData = await GetUserData(chat, Id);
            if (!newData.Status.Error)
            {
                UpdateLocalData(newData.Result);
            }
        }

        /// <summary>
        /// Deletes the user.
        /// <para>
        /// This method deletes the user from the chat.
        /// It will remove the user from all the channels and delete the user's data.
        /// </para>
        /// </summary>
        /// <exception cref="PubNubCCoreException">
        /// This exception might be thrown when any error occurs while deleting the user.
        /// </exception>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// user.DeleteUser();
        /// </code>
        /// </example>
        public async Task DeleteUser()
        {
            await chat.DeleteUser(Id);
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
        /// <exception cref="PubNubCCoreException">
        /// This exception might be thrown when any error occurs while setting the restrictions on the user.
        /// </exception>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// user.SetRestrictions("channel_id", true, false, "Banned from the channel");
        /// </code>
        /// </example>
        public async Task<ChatOperationResult> SetRestriction(string channelId, bool banUser, bool muteUser, string reason)
        {
            return await chat.SetRestriction(Id, channelId, banUser, muteUser, reason);
        }

        public async Task<ChatOperationResult> SetRestriction(string channelId, Restriction restriction)
        {
            return await chat.SetRestriction(Id, channelId, restriction);
        }

        /// <summary>
        /// Gets the restrictions on the user for the channel.
        /// <para>
        /// This method gets the restrictions on the user for the channel.
        /// You can get the restrictions on the user for the channel.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel id for which the restrictions are to be fetched.</param>
        /// <param name="limit">The limit on the number of restrictions to be fetched.</param>
        /// <param name="startTimeToken">The start time token from which the restrictions are to be fetched.</param>
        /// <param name="endTimeToken">The end time token till which the restrictions are to be fetched.</param>
        /// <returns>
        /// The restrictions on the user for the channel.
        /// </returns>
        public async Task<ChatOperationResult<Restriction>> GetChannelRestrictions(Channel channel)
        {
            var result = new ChatOperationResult<Restriction>();
            var membersResult = await chat.PubnubInstance.GetChannelMembers().Channel($"{Chat.INTERNAL_MODERATION_PREFIX}_{channel.Id}").Include(new[]
            {
                PNChannelMemberField.CUSTOM
            }).Filter($"uuid.id == \"{Id}\"").IncludeCount(true).ExecuteAsync();
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

        public async Task<ChatOperationResult<ChannelsRestrictionsWrapper>> GetChannelsRestrictions(string sort = "", int limit = 0,
            PNPageObject page = null)
        {
            var result = new ChatOperationResult<ChannelsRestrictionsWrapper>(){Result = new ChannelsRestrictionsWrapper()};
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
            var membershipsResult = await operation.ExecuteAsync();
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
        /// <c>true</c> if the user is present on the channel; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="PubNubCCoreException">
        /// This exception might be thrown when any error occurs while checking if the user is present on the channel.
        /// </exception>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// if (user.IsPresentOn("channel_id")) {
        ///   // User is present on the channel
        /// }
        /// </code>
        /// </example>
        public async Task<bool> IsPresentOn(string channelId)
        {
            var response = await chat.PubnubInstance.WhereNow().Uuid(Id).ExecuteAsync();
            if (response.Status.Error)
            {
                chat.Logger.Error($"Error when trying to perform IsPresentOn(): {response.Status.ErrorData.Information}");
                return false;
            }
            return response.Result.Channels.Contains(channelId);
        }

        /// <summary>
        /// Gets the list of channels where the user is present.
        /// <para>
        /// This method gets the list of channels where the user is present.
        /// </para>
        /// </summary>
        /// <returns>
        /// The list of channels where the user is present.
        /// </returns>
        /// <remarks>
        /// The list is kept as a list of channel ids.
        /// </remarks>
        /// <exception cref="PubNubCCoreException">
        /// This exception might be thrown when any error occurs while getting the list of channels where the user is present.
        /// </exception>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// var channels = user.WherePresent();
        /// foreach (var channel in channels) {
        ///  Console.WriteLine(channel);
        /// }
        /// </code>
        /// </example>
        public async Task<ChatOperationResult<List<string>>> WherePresent()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the list of memberships of the user.
        /// <para>
        /// This methods gets the list of memberships of the user.
        /// All the relationships of the user with the channels are considered as memberships.
        /// </para>
        /// </summary>
        /// <param name="limit">The limit on the number of memberships to be fetched.</param>
        /// <param name="startTimeToken">The start time token from which the memberships are to be fetched.</param>
        /// <param name="endTimeToken">The end time token till which the memberships are to be fetched.</param>
        /// <returns>
        /// The list of memberships of the user.
        /// </returns>
        /// <exception cref="PubNubCCoreException">
        /// This exception might be thrown when any error occurs while getting the list of memberships of the user.
        /// </exception>
        /// <example>
        /// <code>
        /// var user = // ...;
        /// var memberships = user.GetMemberships(50, "99999999999999999", "00000000000000000");
        /// foreach (var membership in memberships) {
        /// Console.WriteLine(membership.ChannelId);
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="Membership"/>
        public async Task<MembersResponseWrapper> GetMemberships(string filter = "", string sort = "", int limit = 0,
            PNPageObject page = null)
        {
            return await chat.GetUserMemberships(Id, filter, sort, limit, page);
        }
    }
}