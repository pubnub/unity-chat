using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    /// <summary>
    /// Represents a membership of a user in a channel.
    /// <para>
    /// Memberships are relations between users and channels. They are used to determine
    /// which users are allowed to send messages to which channels.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Memberships are created when a user joins a channel and are deleted when a user leaves a channel.
    /// </remarks>
    /// <seealso cref="Chat"/>
    /// <seealso cref="User"/>
    /// <seealso cref="Channel"/>
    public class Membership : UniqueChatEntity
    {
        //Message counts requires a valid timetoken, so this one will be like "0", from beginning of the channel
        internal const long EMPTY_TIMETOKEN = 17000000000000000;
        
        /// <summary>
        /// The user ID of the user that this membership belongs to.
        /// </summary>
        public string UserId { get; }

        /// <summary>
        /// The channel ID of the channel that this membership belongs to.
        /// </summary>
        public string ChannelId { get; }
        
        /// <summary>
        /// The string time token of last read message on the membership channel.
        /// </summary>
        public string LastReadMessageTimeToken => MembershipData.CustomData != null && MembershipData.CustomData.TryGetValue("lastReadMessageTimetoken", out var timeToken) ? timeToken.ToString() : "";

        public ChatMembershipData MembershipData { get; private set; }

        /// <summary>
        /// Event that is triggered when the membership is updated.
        /// <para>
        /// This event is triggered when the membership is updated by the server.
        /// Every time the membership is updated, this event is triggered.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// membership.OnMembershipUpdated += (membership) =>
        /// {
        ///    Console.WriteLine("Membership updated!");
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="Update"/>
        public event Action<Membership> OnMembershipUpdated;

        protected override string UpdateChannelId => ChannelId;

        internal Membership(Chat chat, string userId, string channelId, ChatMembershipData membershipData) : base(chat, userId+channelId)
        {
            UserId = userId;
            ChannelId = channelId;
            UpdateLocalData(membershipData);
        }

        internal void UpdateLocalData(ChatMembershipData newData)
        {
            MembershipData = newData;
        }

        protected override SubscribeCallback CreateUpdateListener()
        {
            return chat.ListenerFactory.ProduceListener(objectEventCallback: delegate(Pubnub pn, PNObjectEventResult e)
            {
                if (ChatParsers.TryParseMembershipUpdate(chat, this, e, out var updatedData))
                {
                    UpdateLocalData(updatedData);
                    OnMembershipUpdated?.Invoke(this);
                }
            });
        }

        /// <summary>
        /// Updates the membership with a ChatMembershipData object.
        /// <para>
        /// This method updates the membership with a ChatMembershipData object. This object can be used to store
        /// additional information about the membership.
        /// </para>
        /// </summary>
        /// <param name="membershipData">The ChatMembershipData object to update the membership with.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <seealso cref="OnMembershipUpdated"/>
        public async Task<ChatOperationResult> Update(ChatMembershipData membershipData)
        {
            var result = (await UpdateMembershipData(membershipData).ConfigureAwait(false)).ToChatOperationResult("Membership.Update()", chat);
            if (!result.Error)
            {
                UpdateLocalData(membershipData);
            }
            return result;
        }

        internal async Task<PNResult<PNMembershipsResult>> UpdateMembershipData(ChatMembershipData membershipData)
        {
            return await chat.PubnubInstance.ManageMemberships().Uuid(UserId).Set(new List<PNMembership>()
            {
                new()
                {
                    Channel = ChannelId,
                    Custom = membershipData.CustomData,
                    Status = membershipData.Status,
                    Type = membershipData.Type
                }
            }).Include(new[]
            {
                PNMembershipField.TYPE,
                PNMembershipField.CUSTOM,
                PNMembershipField.STATUS,
                PNMembershipField.CHANNEL,
                PNMembershipField.CHANNEL_CUSTOM,
                PNMembershipField.CHANNEL_TYPE,
                PNMembershipField.CHANNEL_STATUS,
            }).ExecuteAsync().ConfigureAwait(false);
        }
        
        internal static async Task<PNResult<PNMembershipsResult>> UpdateMembershipsData(Chat chat, string userId, List<Membership> memberships)
        {
            var pnMemberships = memberships.Select(membership => new PNMembership()
            {
                Channel = membership.ChannelId,
                Custom = membership.MembershipData.CustomData,
                Status = membership.MembershipData.Status,
                Type = membership.MembershipData.Type
            }).ToList();
            return await chat.PubnubInstance.SetMemberships().Uuid(userId).Channels(pnMemberships).Include(new[]
            {
                PNMembershipField.TYPE,
                PNMembershipField.CUSTOM,
                PNMembershipField.STATUS,
                PNMembershipField.CHANNEL,
                PNMembershipField.CHANNEL_CUSTOM,
                PNMembershipField.CHANNEL_TYPE,
                PNMembershipField.CHANNEL_STATUS
            }).ExecuteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the last read message for this membership.
        /// <para>
        /// Updates the membership to mark the specified message as the last one read by the user.
        /// This is used for tracking read receipts and unread message counts.
        /// </para>
        /// </summary>
        /// <param name="message">The message to mark as last read.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <seealso cref="SetLastReadMessageTimeToken"/>
        /// <seealso cref="GetUnreadMessagesCount"/>
        public async Task<ChatOperationResult> SetLastReadMessage(Message message)
        {
            return await SetLastReadMessageTimeToken(message.TimeToken).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Sets the last read message time token for this membership.
        /// <para>
        /// Updates the membership to mark the message with the specified time token as the last one read by the user.
        /// This is used for tracking read receipts and unread message counts.
        /// </para>
        /// </summary>
        /// <param name="timeToken">The time token of the message to mark as last read.</param>
        /// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
        /// <seealso cref="SetLastReadMessage"/>
        /// <seealso cref="GetUnreadMessagesCount"/>
        public async Task<ChatOperationResult> SetLastReadMessageTimeToken(string timeToken)
        {
            var result = new ChatOperationResult("Membership.SetLastReadMessageTimeToken()", chat);
            MembershipData.CustomData ??= new Dictionary<string, object>();
            MembershipData.CustomData["lastReadMessageTimetoken"] = timeToken;
            var update = await UpdateMembershipData(MembershipData).ConfigureAwait(false);
            if (result.RegisterOperation(update))
            {
                return result;
            }
            result.RegisterOperation(await chat.EmitEvent(PubnubChatEventType.Receipt, ChannelId,
                $"{{\"messageTimetoken\": \"{timeToken}\"}}").ConfigureAwait(false));
            return result;
        }
        
        /// <summary>
        /// Gets the count of unread messages for this membership.
        /// <para>
        /// Calculates the number of messages that have been sent to the channel since the last read message time token.
        /// </para>
        /// </summary>
        /// <returns>A ChatOperationResult with the number of unread messages</returns>
        /// <seealso cref="SetLastReadMessage"/>
        /// <seealso cref="SetLastReadMessageTimeToken"/>
        public async Task<ChatOperationResult<long>> GetUnreadMessagesCount()
        {
            var result = new ChatOperationResult<long>("Membership.GetUnreadMessagesCount()", chat);
            if (!long.TryParse(LastReadMessageTimeToken, out var lastRead))
            {
                result.Error = true;
                result.Exception = new PNException("LastReadMessageTimeToken is not a valid time token!");
                return result;
            }
            lastRead = lastRead == 0 ? EMPTY_TIMETOKEN : lastRead;
            var countsResponse = await chat.PubnubInstance.MessageCounts().Channels(new[] { ChannelId })
                .ChannelsTimetoken(new[] { lastRead }).ExecuteAsync().ConfigureAwait(false);
            if (result.RegisterOperation(countsResponse))
            {
                return result;
            }
            result.Result = countsResponse.Result.Channels[ChannelId];
            return result;
        }

        /// <summary>
        /// Refreshes the membership data from the server.
        /// <para>
        /// Fetches the latest membership information from the server and updates the local data.
        /// This is useful when you want to ensure you have the most up-to-date membership information.
        /// </para>
        /// </summary>
        /// <returns>A ChatOperationResult indicating the success or failure of the refresh operation.</returns>
        public override async Task<ChatOperationResult> Refresh()
        {
            return await chat.GetChannelMemberships(ChannelId, filter:$"uuid.id == \"{UserId}\"").ConfigureAwait(false);
        }
    }
}