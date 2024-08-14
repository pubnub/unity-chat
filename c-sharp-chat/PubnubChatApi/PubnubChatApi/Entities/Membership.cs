using System;
using System.Runtime.InteropServices;
using System.Text;
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
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern void pn_membership_delete(IntPtr membership);

        [DllImport("pubnub-chat")]
        private static extern void pn_membership_get_user_id(
            IntPtr membership,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_membership_get_channel_id(
            IntPtr membership,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_membership_update_dirty(
            IntPtr membership,
            string custom_object_json);

        [DllImport("pubnub-chat")]
        private static extern int pn_membership_last_read_message_timetoken(IntPtr membership, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_membership_set_last_read_message_timetoken(IntPtr membership, string timetoken);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_membership_set_last_read_message(IntPtr membership, IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern int pn_membership_get_unread_messages_count(IntPtr membership);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_membership_update_with_base(IntPtr membership,
            IntPtr base_membership);

        #endregion

        /// <summary>
        /// The user ID of the user that this membership belongs to.
        /// </summary>
        public string UserId
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_membership_get_user_id(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The channel ID of the channel that this membership belongs to.
        /// </summary>
        public string ChannelId
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_membership_get_channel_id(pointer, buffer);
                return buffer.ToString();
            }
        }

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

        private Chat chat;
        
        public override void StartListeningForUpdates()
        {
            //TODO: hacky way to subscribe to this channel
            chat.ListenForEvents(ChannelId, PubnubChatEventType.Custom);
        }

        internal Membership(Chat chat, IntPtr membershipPointer, string membershipId) : base(membershipPointer, membershipId)
        {
            this.chat = chat;
        }

        internal static string GetMembershipIdFromPtr(IntPtr membershipPointer)
        {
            var userIdBuffer = new StringBuilder(512);
            pn_membership_get_user_id(membershipPointer, userIdBuffer);
            var userId = userIdBuffer.ToString();
            var channelIdBuffer = new StringBuilder(512);
            pn_membership_get_channel_id(membershipPointer, channelIdBuffer);
            var channelId = channelIdBuffer.ToString();
            return userId + channelId;
        }

        internal void BroadcastMembershipUpdate()
        {
            OnMembershipUpdated?.Invoke(this);
        }
        
        internal override void UpdateWithPartialPtr(IntPtr partialPointer)
        {
            var newFullPointer = pn_membership_update_with_base(partialPointer, pointer);
            CUtilities.CheckCFunctionResult(newFullPointer);
            UpdatePointer(newFullPointer);
        }

        /// <summary>
        /// Updates the membership with a custom JSON object.
        /// <para>
        /// This method updates the membership with a custom JSON object. This object can be used to store
        /// additional information about the membership.
        /// </para>
        /// </summary>
        /// <param name="customJsonObject">The custom JSON object to update the membership with.</param>
        /// <example>
        /// <code>
        /// membership.Update("{\"key\": \"value\"}");
        /// </code>
        /// </example>
        /// <seealso cref="OnMembershipUpdated"/>
        public void Update(string customJsonObject)
        {
            var newPointer = pn_membership_update_dirty(pointer, customJsonObject);
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }

        public string GetLastReadMessageTimeToken()
        {
            var buffer = new StringBuilder(128);
            CUtilities.CheckCFunctionResult(pn_membership_last_read_message_timetoken(pointer, buffer));
            return buffer.ToString();
        }

        public void SetLastReadMessage(Message message)
        {
            var newPointer = pn_membership_set_last_read_message(pointer, message.Pointer);
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }
        
        public void SetLastReadMessageTimeToken(string timeToken)
        {
            var newPointer = pn_membership_set_last_read_message_timetoken(pointer, timeToken);
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }

        public int GetUnreadMessagesCount()
        {
            var result = pn_membership_get_unread_messages_count(pointer);
            CUtilities.CheckCFunctionResult(result);
            return result;
        }

        protected override void DisposePointer()
        {
            pn_membership_delete(pointer);
        }
    }
}