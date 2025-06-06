using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
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
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern void pn_channel_delete(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_connect(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_disconnect(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_join(IntPtr channel, string additional_params);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_leave(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_set_restrictions(IntPtr channel, string user_id, bool ban_user,
            bool mute_user, string reason);

        [DllImport("pubnub-chat")]
        private static extern void pn_channel_get_channel_id(
            IntPtr channel,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_get_user_restrictions(
            IntPtr channel,
            IntPtr user,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_channel_get_data_channel_name(
            IntPtr channel,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_channel_get_data_description(
            IntPtr channel,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_channel_get_data_custom_data_json(
            IntPtr channel,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_channel_get_data_updated(
            IntPtr channel,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_channel_get_data_status(
            IntPtr channel,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_channel_get_data_type(
            IntPtr channel,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_is_present(IntPtr channel, string user_id);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_who_is_present(IntPtr channel, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_invite_user(IntPtr channel, IntPtr user);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_invite_multiple(IntPtr channel, IntPtr[] users, int users_length,
            StringBuilder result_json);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_start_typing(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_stop_typing(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_pin_message(IntPtr channel, IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_unpin_message(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_get_pinned_message(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_create_message_draft_dirty(IntPtr channel,
            int user_suggestion_source,
            bool is_typing_indicator_triggered,
            int user_limit,
            int channel_limit);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_emit_user_mention(IntPtr channel, string user_id, string timetoken,
            string text);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_update_with_base(IntPtr channel, IntPtr base_channel);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_get_user_suggestions(IntPtr channel, string text, int limit,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_send_text_dirty(
            IntPtr channel,
            string message,
            bool store_in_history,
            bool send_by_post,
            string meta,
            int mentioned_users_length,
            int[] mentioned_users_indexes,
            IntPtr[] mentioned_users,
            IntPtr quoted_message);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_get_users_restrictions(IntPtr channel, string sort, int limit, string next,
            string prev, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_stream_read_receipts(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_stream_message_reports(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_stream_updates(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_get_typing(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_stream_presence(IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_join_with_membership_data(
            IntPtr channel,
            string membership_custom_json,
            string membership_type,
            string membership_status
        );

        #endregion

        /// <summary>
        /// The name of the channel.
        ///
        /// <para>
        /// The name of the channel that is human meaningful.
        /// </para>
        /// </summary>
        /// <value>The name of the channel.</value>
        public string Name
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_channel_get_data_channel_name(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The description of the channel.
        ///
        /// <para>
        /// The description that allows users to understand the purpose of the channel.
        /// </para>
        public string Description
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_channel_get_data_description(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The custom data of the channel.
        ///
        /// <para>
        /// The custom data that can be used to store additional information about the channel.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The custom data is stored in JSON format.
        /// </remarks>
        public string CustomDataJson
        {
            get
            {
                var buffer = new StringBuilder(2048);
                pn_channel_get_data_custom_data_json(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The information about the last update of the channel.
        /// <para>
        /// The time when the channel was last updated.
        /// </para>
        /// </summary>
        public string Updated
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_channel_get_data_updated(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The status of the channel.
        /// <para>
        /// The last status response received from the server.
        /// </para>
        /// </summary>
        public string Status
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_channel_get_data_status(pointer, buffer);
                return buffer.ToString();
            }
        }

        /// <summary>
        /// The type of the channel.
        /// <para>
        /// The type of the response received from the server when the channel was created.
        /// </para>
        /// </summary>
        public string Type
        {
            get
            {
                var buffer = new StringBuilder(512);
                pn_channel_get_data_type(pointer, buffer);
                return buffer.ToString();
            }
        }

        protected Chat chat;
        private IntPtr customEventsListeningHandle;
        private IntPtr reportEventsListeningHandle;
        private IntPtr readReceiptsListeningHandle;
        private IntPtr typingListeningHandle;
        private IntPtr presenceListeningHandle;
        protected IntPtr connectionHandle;
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

        internal Channel(Chat chat, string channelId, IntPtr channelPointer) : base(channelPointer, channelId)
        {
            this.chat = chat;
        }

        protected override IntPtr StreamUpdates()
        {
            return pn_channel_stream_updates(pointer);
        }

        public async void SetListeningForCustomEvents(bool listen)
        {
            customEventsListeningHandle = await SetListening(customEventsListeningHandle, listen,
                () => chat.ListenForEvents(Id, PubnubChatEventType.Custom));
        }

        internal void BroadcastCustomEvent(ChatEvent chatEvent)
        {
            OnCustomEvent?.Invoke(chatEvent);
        }

        public async void SetListeningForReportEvents(bool listen)
        {
            reportEventsListeningHandle = await SetListening(reportEventsListeningHandle, listen,
                () => pn_channel_stream_message_reports(pointer));
        }

        internal void BroadcastReportEvent(ChatEvent chatEvent)
        {
            OnReportEvent?.Invoke(chatEvent);
        }

        public async void SetListeningForReadReceiptsEvents(bool listen)
        {
            readReceiptsListeningHandle = await SetListening(readReceiptsListeningHandle, listen,
                () => pn_channel_stream_read_receipts(pointer));
        }

        public async void SetListeningForTyping(bool listen)
        {
            typingListeningHandle = await SetListening(typingListeningHandle, listen,
                () => pn_channel_get_typing(pointer));
        }

        public async void SetListeningForPresence(bool listen)
        {
            presenceListeningHandle = await SetListening(presenceListeningHandle, listen,
                () => pn_channel_stream_presence(pointer));
        }

        internal static string GetChannelIdFromPtr(IntPtr channelPointer)
        {
            var buffer = new StringBuilder(512);
            pn_channel_get_channel_id(channelPointer, buffer);
            return buffer.ToString();
        }

        internal void BroadcastMessageReceived(Message message)
        {
            OnMessageReceived?.Invoke(message);
        }

        internal void BroadcastReadReceipt(Dictionary<string, List<string>?> readReceiptEventData)
        {
            OnReadReceiptEvent?.Invoke(readReceiptEventData);
        }

        internal override void UpdateWithPartialPtr(IntPtr partialPointer)
        {
            var newFullPointer = pn_channel_update_with_base(partialPointer, pointer);
            CUtilities.CheckCFunctionResult(newFullPointer);
            UpdatePointer(newFullPointer);
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
            CUtilities.CheckCFunctionResult(await Task.Run(() =>
                pn_channel_emit_user_mention(pointer, userId, timeToken, text)));
        }

        public async Task StartTyping()
        {
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_channel_start_typing(pointer)));
        }

        public async Task StopTyping()
        {
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_channel_stop_typing(pointer)));
        }

        public virtual async Task PinMessage(Message message)
        {
            var newPointer = await Task.Run(() => pn_channel_pin_message(pointer, message.Pointer));
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }

        public virtual async Task UnpinMessage()
        {
            var newPointer = await Task.Run(() => pn_channel_unpin_message(pointer));
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
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
            var pinnedMessagePointer = pn_channel_get_pinned_message(pointer);
            if (pinnedMessagePointer != IntPtr.Zero)
            {
                var id = Message.GetMessageIdFromPtr(pinnedMessagePointer);
                //TODO: this loose wrapper will cause problems of it's own but I don't see another solution for now
                //TODO: will be improved with the final ThreadMessage/Message divorce anyway
                pinnedMessage = new Message(chat, pinnedMessagePointer, id);
                return true;
            }
            else
            {
                pinnedMessage = null;
                Debug.WriteLine($"Error when fetching pinned message: {CUtilities.GetErrorMessage()}");
                return false;
            }
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

        public async Task<List<Membership>> GetUserSuggestions(string text, int limit = 10)
        {
            var buffer = new StringBuilder(2048);
            CUtilities.CheckCFunctionResult(await Task.Run(() =>
                pn_channel_get_user_suggestions(pointer, text, limit, buffer)));
            var resultJson = buffer.ToString();
            if (!CUtilities.IsValidJson(resultJson))
            {
                return new List<Membership>();
            }

            var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, IntPtr[]>>(resultJson);
            if (jsonDict == null || !jsonDict.TryGetValue("value", out var pointers) || pointers == null)
            {
                return new List<Membership>();
            }

            return PointerParsers.ParseJsonMembershipPointers(chat, pointers);
        }

        /// <summary>
        /// Creates a new MessageDraft.
        /// </summary>
        /// <param name="userSuggestionSource">Source of the user suggestions</param>
        /// <param name="isTypingIndicatorTriggered">Typing indicator trigger status.</param>
        /// <param name="userLimit">User limit.</param>
        /// <param name="channelLimit">Channel limit.</param>
        /// <returns></returns>
        public MessageDraft CreateMessageDraft(UserSuggestionSource userSuggestionSource = UserSuggestionSource.GLOBAL,
            bool isTypingIndicatorTriggered = true, int userLimit = 10, int channelLimit = 10)
        {
            var draftPointer = pn_channel_create_message_draft_dirty(
                pointer, (int)userSuggestionSource, isTypingIndicatorTriggered, userLimit, channelLimit);
            CUtilities.CheckCFunctionResult(draftPointer);
            return new MessageDraft(draftPointer);
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
        public async void Connect()
        {
            if (connectionHandle != IntPtr.Zero)
            {
                return;
            }

            connectionHandle = await SetListening(connectionHandle, true, () => pn_channel_connect(pointer));
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
            if (connectionHandle != IntPtr.Zero)
            {
                return;
            }

            if (membershipData == null)
            {
                connectionHandle =
                    await SetListening(connectionHandle, true, () => pn_channel_join(pointer, string.Empty));
            }
            else
            {
                connectionHandle = await SetListening(connectionHandle, true,
                    () => pn_channel_join_with_membership_data(pointer, membershipData.CustomDataJson,
                        membershipData.Type, membershipData.Status));
            }
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
            if (connectionHandle == IntPtr.Zero || pointer == IntPtr.Zero)
            {
                return;
            }

            CUtilities.CheckCFunctionResult(pn_channel_disconnect(pointer));
            pn_callback_handle_dispose(connectionHandle);
            connectionHandle = IntPtr.Zero;
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
            if (connectionHandle == IntPtr.Zero || pointer == IntPtr.Zero)
            {
                return;
            }

            var connectionHandleCopy = connectionHandle;
            connectionHandle = IntPtr.Zero;
            CUtilities.CheckCFunctionResult(await Task.Run(() =>
            {
                if (pointer == IntPtr.Zero)
                {
                    return 0;
                }

                pn_channel_leave(pointer);
                pn_callback_handle_dispose(connectionHandleCopy);
                return 0;
            }));
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
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_channel_set_restrictions(pointer, userId, banUser,
                muteUser,
                reason)));
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
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_channel_send_text_dirty(
                pointer,
                message,
                sendTextParams.StoreInHistory,
                sendTextParams.SendByPost,
                sendTextParams.Meta,
                sendTextParams.MentionedUsers.Count,
                sendTextParams.MentionedUsers.Keys.ToArray(),
                sendTextParams.MentionedUsers.Values.Select(x => x.Pointer).ToArray(),
                sendTextParams.QuotedMessage == null ? IntPtr.Zero : sendTextParams.QuotedMessage.Pointer)));
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
            var buffer = new StringBuilder(4096);
            CUtilities.CheckCFunctionResult(await Task.Run(() =>
                pn_channel_get_user_restrictions(pointer, user.Pointer, buffer)));
            var restrictionJson = buffer.ToString();
            var restriction = new Restriction();
            if (CUtilities.IsValidJson(restrictionJson))
            {
                restriction = JsonConvert.DeserializeObject<Restriction>(restrictionJson);
            }

            return restriction;
        }

        public async Task<UsersRestrictionsWrapper> GetUsersRestrictions(string sort = "", int limit = 0,
            Page page = null)
        {
            page ??= new Page();
            var buffer = new StringBuilder(4096);
            CUtilities.CheckCFunctionResult(
                await Task.Run(() =>
                    pn_channel_get_users_restrictions(pointer, sort, limit, page.Next, page.Previous, buffer)));
            var restrictionsJson = buffer.ToString();
            if (!CUtilities.IsValidJson(restrictionsJson))
            {
                return new UsersRestrictionsWrapper();
            }

            var wrapper = JsonConvert.DeserializeObject<UsersRestrictionsWrapper>(restrictionsJson);
            wrapper ??= new UsersRestrictionsWrapper();
            return wrapper;
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
            var result = await Task.Run(() => pn_channel_is_present(pointer, userId));
            CUtilities.CheckCFunctionResult(result);
            return result == 1;
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
            var buffer = new StringBuilder(4096);
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_channel_who_is_present(pointer, buffer)));
            var jsonResult = buffer.ToString();
            var ret = new List<string>();
            if (CUtilities.IsValidJson(jsonResult))
            {
                ret = JsonConvert.DeserializeObject<List<string>>(jsonResult);
                ret ??= new List<string>();
            }

            return ret;
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
        public async Task<MembersResponseWrapper> GetMemberships(string filter = "", string sort = "", int limit = 0,
            Page page = null)
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

        public async Task<Membership> Invite(User user)
        {
            var membershipPointer = await Task.Run(() => pn_channel_invite_user(pointer, user.Pointer));
            CUtilities.CheckCFunctionResult(membershipPointer);
            var membershipId = Membership.GetMembershipIdFromPtr(membershipPointer);
            chat.TryGetMembership(membershipId, membershipPointer, out var membership);
            return membership;
        }

        public async Task<List<Membership>> InviteMultiple(List<User> users)
        {
            var buffer = new StringBuilder(8192);
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_channel_invite_multiple(pointer,
                users.Select(x => x.Pointer).ToArray(),
                users.Count, buffer)));
            return PointerParsers.ParseJsonMembershipPointers(chat, buffer.ToString());
        }

        protected override async Task CleanupConnectionHandles()
        {
            await base.CleanupConnectionHandles();
            customEventsListeningHandle = await SetListening(customEventsListeningHandle, false,
                () => chat.ListenForEvents(Id, PubnubChatEventType.Custom));
            reportEventsListeningHandle = await SetListening(reportEventsListeningHandle, false,
                () => pn_channel_stream_message_reports(pointer));
            readReceiptsListeningHandle = await SetListening(readReceiptsListeningHandle, false,
                () => pn_channel_stream_read_receipts(pointer));
            typingListeningHandle = await SetListening(typingListeningHandle, false,
                () => pn_channel_get_typing(pointer));
            presenceListeningHandle = await SetListening(presenceListeningHandle, false,
                () => pn_channel_stream_presence(pointer));
            Disconnect();
        }

        protected override void DisposePointer()
        {
            pn_channel_delete(pointer);
            pointer = IntPtr.Zero;
        }
    }
}