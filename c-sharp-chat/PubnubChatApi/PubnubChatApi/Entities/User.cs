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

        private Chat chat;

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
        
        public event Action<ChatEvent> OnMentionEvent;
        public event Action<ChatEvent> OnInviteEvent;
        public event Action<ChatEvent> OnModerationEvent;
        
        internal User(Chat chat, string userId, ChatUserData chatUserData) : base(userId)
        {
            UpdateLocalData(chatUserData);
            this.chat = chat;
        }
        
        public async void SetListeningForMentionEvents(bool listen)
        {
            throw new NotImplementedException();
        }

        internal void BroadcastMentionEvent(ChatEvent chatEvent)
        {
            OnMentionEvent?.Invoke(chatEvent);
        }

        public async void SetListeningForInviteEvents(bool listen)
        {
            throw new NotImplementedException();
        }
        
        internal void BroadcastInviteEvent(ChatEvent chatEvent)
        {
            OnInviteEvent?.Invoke(chatEvent);
        }

        public async void SetListeningForModerationEvents(bool listen)
        {
            throw new NotImplementedException();
        }
        
        internal void BroadcastModerationEvent(ChatEvent chatEvent)
        {
            OnModerationEvent?.Invoke(chatEvent);
        }

        internal void BroadcastUserUpdate()
        {
            OnUserUpdated?.Invoke(this);
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

        internal static async Task<bool> UpdateUserData(Chat chat, string userId, ChatUserData chatUserData)
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
            var result = await operation.ExecuteAsync();
            if (result.Status.Error)
            {
                chat.PubnubInstance.PNConfig.Logger.Error($"Error when trying to update user data for user \"{userId}\": {result.Status.ErrorData.Information}");
                return false;
            }
            return true;
        }
        
        internal static async Task<ChatUserData?> GetUserData(Chat chat, string userId)
        {
            var result = await chat.PubnubInstance.GetUuidMetadata().Uuid(userId).IncludeCustom(true).ExecuteAsync();
            if (result.Status.Error)
            {
                chat.PubnubInstance.PNConfig.Logger.Error($"Error when trying to Resync() User \"{userId}\": {result.Status.ErrorData.Information}");
                return null;
            }
            try
            {
                return (ChatUserData)result.Result;
            }
            catch (Exception e)
            {
                chat.PubnubInstance.PNConfig.Logger.Error($"Error when trying to parse data for User \"{userId}\": {e.Message}");
                return null;
            }
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
            UpdateLocalData(newData);
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
        public async Task SetRestriction(string channelId, bool banUser, bool muteUser, string reason)
        {
            await chat.SetRestriction(Id, channelId, banUser, muteUser, reason);
        }

        public async Task SetRestriction(string channelId, Restriction restriction)
        {
            await chat.SetRestriction(Id, channelId, restriction);
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
        /// <exception cref="PubNubCCoreException">
        /// This exception might be thrown when any error occurs while getting the restrictions on the user for the channel.
        /// 
        public async Task<Restriction> GetChannelRestrictions(Channel channel)
        {
            throw new NotImplementedException();
        }

        public async Task<ChannelsRestrictionsWrapper> GetChannelsRestrictions(string sort = "", int limit = 0,
            Page page = null)
        {
            throw new NotImplementedException();
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
        public async Task<List<string>> WherePresent()
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