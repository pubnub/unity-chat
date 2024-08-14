using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Entities.Events;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
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
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_new(
            string publish,
            string subscribe,
            string user_id,
            string auth_key,
            int typing_timeout,
            int typing_timeout_difference);

        [DllImport("pubnub-chat")]
        private static extern void pn_chat_delete(IntPtr chat);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_create_public_conversation_dirty(IntPtr chat,
            string channel_id,
            string channel_name,
            string channel_description,
            string channel_custom_data_json,
            string channel_updated,
            string channel_status,
            string channel_type);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_update_channel_dirty(IntPtr chat,
            string channel_id,
            string channel_name,
            string channel_description,
            string channel_custom_data_json,
            string channel_updated,
            string channel_status,
            string channel_type);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_set_restrictions(IntPtr chat,
            string user_id,
            string channel_id,
            bool ban_user,
            bool mute_user,
            string reason);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_delete_channel(IntPtr chat, string channel_id);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_create_user_dirty(IntPtr chat,
            string user_id,
            string user_name,
            string external_id,
            string profile_url,
            string email,
            string custom_data_json,
            string status,
            string type);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_update_user_dirty(IntPtr chat,
            string user_id,
            string user_name,
            string external_id,
            string profile_url,
            string email,
            string custom_data_json,
            string status,
            string type);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_delete_user(IntPtr chat, string user_id);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_get_updates(IntPtr chat, StringBuilder messages_json);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_deserialize_message(IntPtr chat, IntPtr message);
        
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_deserialize_thread_message(IntPtr chat, IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_deserialize_channel(IntPtr chat, IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_deserialize_user(IntPtr chat, IntPtr user);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_deserialize_membership(IntPtr chat, IntPtr membership);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_deserialize_message_update(IntPtr chat, IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern int pn_deserialize_event(IntPtr eventPtr, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_deserialize_presence(IntPtr presence, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern void pn_dispose_message(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_get_users(
            IntPtr chat,
            string filter,
            string sort,
            int limit,
            string next,
            string prev,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_get_user(
            IntPtr chat,
            string user_id);

        [DllImport("pubnub-chat")]
        public static extern IntPtr pn_chat_get_channel(
            IntPtr chat,
            string channel_id);

        [DllImport("pubnub-chat")]
        private static extern int pn_user_get_memberships(
            IntPtr user,
            string filter,
            string sort,
            int limit,
            string next,
            string prev,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_get_members(
            IntPtr channel,
            string filter,
            string sort,
            int limit,
            string next,
            string prev,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_channel_get_message(IntPtr channel, string timetoken);

        [DllImport("pubnub-chat")]
        private static extern int pn_channel_get_history(
            IntPtr channel,
            string start,
            string end,
            int count,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_get_channels(
            IntPtr chat,
            string filter,
            string sort,
            int limit,
            string next,
            string prev,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_listen_for_events(
            IntPtr chat,
            string channel_id,
            byte event_type,
            StringBuilder result_json);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_create_direct_conversation_dirty(
            IntPtr chat,
            IntPtr user, string channel_id,
            string channel_name,
            string channel_description,
            string channel_custom_data_json,
            string channel_updated,
            string channel_status,
            string channel_type);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_create_group_conversation_dirty(
            IntPtr chat,
            IntPtr[] users,
            int users_length, string channel_id,
            string channel_name,
            string channel_description,
            string channel_custom_data_json,
            string channel_updated,
            string channel_status,
            string channel_type);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_get_created_channel_wrapper_channel(
            IntPtr wrapper);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_get_created_channel_wrapper_host_membership(
            IntPtr wrapper);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_get_created_channel_wrapper_invited_memberships(
            IntPtr wrapper, StringBuilder result_json);

        [DllImport("pubnub-chat")]
        private static extern void pn_chat_dispose_created_channel_wrapper(IntPtr wrapper);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_forward_message(IntPtr chat, IntPtr message, IntPtr channel);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_emit_event(IntPtr chat, byte chat_event_type, string channel_id,
            string payload);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_message_create_thread(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_message_get_thread(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern int pn_message_remove_thread(IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_get_unread_messages_counts(
            IntPtr chat,
            string filter,
            string sort,
            int limit,
            string next,
            string prev,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_get_channel_suggestions(IntPtr chat, string text, int limit,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_mark_all_messages_as_read(
            IntPtr chat,
            string filter,
            string sort,
            int limit,
            string next,
            string prev,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_get_events_history(
            IntPtr chat,
            string channel_id,
            string start_timetoken,
            string end_timetoken,
            int count,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_chat_get_user_suggestions(IntPtr chat, string text, int limit,
            StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_current_user(IntPtr chat);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_deserialize_thread_message_update(IntPtr chat, IntPtr message_update);

        #endregion

        private IntPtr chatPointer;
        internal IntPtr Pointer => chatPointer;
        private Dictionary<string, Channel> channelWrappers = new();
        private Dictionary<string, User> userWrappers = new();
        private Dictionary<string, Membership> membershipWrappers = new();
        private Dictionary<string, Message> messageWrappers = new();
        private bool fetchUpdates = true;
        private Thread fetchUpdatesThread;

        public event Action<ChatEvent> OnReportEvent;
        public event Action<ChatEvent> OnModerationEvent;
        public event Action<ChatEvent> OnTypingEvent;
        public event Action<ChatEvent> OnReadReceiptEvent;
        public event Action<ChatEvent> OnMentionEvent;
        public event Action<ChatEvent> OnInviteEvent;
        public event Action<ChatEvent> OnCustomEvent;
        public event Action<ChatEvent> OnAnyEvent;

        public ChatAccessManager ChatAccessManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chat"/> class.
        /// <para>
        /// Creates a new chat instance.
        /// </para>
        /// </summary>
        /// <param name="config">Config with PubNub keys and values</param>
        /// <remarks>
        /// The constructor initializes the chat instance with the provided keys and user ID from the Config.
        /// </remarks>
        public Chat(PubnubChatConfig config)
        {
            chatPointer = pn_chat_new(config.PublishKey, config.SubscribeKey, config.UserId, config.AuthKey,
                config.TypingTimeout, config.TypingTimeoutDifference);
            CUtilities.CheckCFunctionResult(chatPointer);

            ChatAccessManager = new ChatAccessManager(chatPointer);

            fetchUpdatesThread = new Thread(FetchUpdatesLoop) { IsBackground = true };
            fetchUpdatesThread.Start();
        }

        #region Updates handling

        private void FetchUpdatesLoop()
        {
            while (fetchUpdates)
            {
                var updates = GetUpdates();
                ParseJsonUpdatePointers(updates);
                Thread.Sleep(500);
            }
        }

        internal void ParseJsonUpdatePointers(string jsonPointers)
        {
            if (!string.IsNullOrEmpty(jsonPointers) && jsonPointers != "[]")
            {
                Debug.WriteLine($"Received pointers JSON to parse: {jsonPointers}");

                var pubnubV2MessagePointers = JsonConvert.DeserializeObject<IntPtr[]>(jsonPointers);
                if (pubnubV2MessagePointers == null)
                {
                    return;
                }

                foreach (var pointer in pubnubV2MessagePointers)
                {
                    //Events (json for now)
                    var allEventsBuffer = new StringBuilder(16384);
                    if (pn_deserialize_event(pointer, allEventsBuffer) != -1)
                    {
                        Debug.WriteLine("Deserialized event");

                        var eventJson = allEventsBuffer.ToString();
                        if (!CUtilities.IsValidJson(eventJson))
                        {
                            pn_dispose_message(pointer);
                            continue;
                        }

                        var chatEvent = JsonConvert.DeserializeObject<ChatEvent>(eventJson);
                        var failedToInvoke = false;
                        //TODO: not a big fan of this big-ass switch
                        switch (chatEvent.Type)
                        {
                            case PubnubChatEventType.Typing:
                                if (TryGetChannel(chatEvent.ChannelId, out var typingChannel)
                                    && typingChannel.TryParseAndBroadcastTypingEvent(chatEvent))
                                {
                                    OnTypingEvent?.Invoke(chatEvent);
                                }
                                else
                                {
                                    failedToInvoke = true;
                                }

                                break;
                            case PubnubChatEventType.Report:
                                OnReportEvent?.Invoke(chatEvent);
                                break;
                            case PubnubChatEventType.Receipt:
                                OnReadReceiptEvent?.Invoke(chatEvent);
                                if (TryGetChannel(chatEvent.ChannelId, out var readReceiptChannel))
                                {
                                    readReceiptChannel.BroadcastReadReceipt(chatEvent);
                                }

                                break;
                            case PubnubChatEventType.Mention:
                                OnMentionEvent?.Invoke(chatEvent);
                                break;
                            case PubnubChatEventType.Invite:
                                OnInviteEvent?.Invoke(chatEvent);
                                break;
                            case PubnubChatEventType.Custom:
                                OnCustomEvent?.Invoke(chatEvent);
                                break;
                            case PubnubChatEventType.Moderation:
                                OnModerationEvent?.Invoke(chatEvent);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (!failedToInvoke)
                        {
                            OnAnyEvent?.Invoke(chatEvent);
                        }

                        pn_dispose_message(pointer);
                        continue;
                    }
                    
                    //New Thread message
                    var threadMessagePointer = pn_deserialize_thread_message(chatPointer, pointer);
                    if (threadMessagePointer != IntPtr.Zero)
                    {
                        Debug.WriteLine("Deserialized new thread message");

                        var id = Message.GetChannelIdFromMessagePtr(threadMessagePointer);
                        if (channelWrappers.TryGetValue(id, out var channel))
                        {
                            Debug.WriteLine("AAAAAAAAAAAAAAAA");
                            var timeToken = Message.GetMessageIdFromPtr(threadMessagePointer);
                            var message = new ThreadMessage(this, threadMessagePointer, timeToken);
                            messageWrappers[timeToken] = message;
                            channel.BroadcastMessageReceived(message);
                        }
                        else
                        {
                            Debug.WriteLine("BBBBBBBBBBB");
                        }

                        pn_dispose_message(pointer);
                        continue;
                    }

                    //New message
                    var messagePointer = pn_deserialize_message(chatPointer, pointer);
                    if (messagePointer != IntPtr.Zero)
                    {
                        Debug.WriteLine("Deserialized new message");

                        var id = Message.GetChannelIdFromMessagePtr(messagePointer);
                        if (channelWrappers.TryGetValue(id, out var channel))
                        {
                            var timeToken = Message.GetMessageIdFromPtr(messagePointer);
                            var message = new Message(this, messagePointer, timeToken);
                            messageWrappers[timeToken] = message;
                            channel.BroadcastMessageReceived(message);
                        }

                        pn_dispose_message(pointer);
                        continue;
                    }

                    //Updated existing thread message
                    var updatedThreadMessagePointer = pn_deserialize_thread_message_update(chatPointer, pointer);
                    if (updatedThreadMessagePointer != IntPtr.Zero)
                    {
                        Debug.WriteLine("Deserialized thread message update");
                        var id = Message.GetMessageIdFromPtr(updatedThreadMessagePointer);
                        if (messageWrappers.TryGetValue(id, out var existingMessageWrapper))
                        {
                            Debug.WriteLine("KURWA");
                            if (existingMessageWrapper is ThreadMessage existingThreadMessageWrapper)
                            {
                                Debug.WriteLine("MAĆ");
                                existingThreadMessageWrapper.UpdateWithPartialPtr(updatedThreadMessagePointer);
                                existingThreadMessageWrapper.BroadcastMessageUpdate();
                            }
                            else
                            {
                                Debug.WriteLine(
                                    "Thread message was stored as a regular message - SHOULD NEVER HAPPEN!");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("CHUJ");
                        }

                        pn_dispose_message(pointer);
                        continue;
                    }

                    //Updated existing message
                    var updatedMessagePointer = pn_deserialize_message_update(chatPointer, pointer);
                    if (updatedMessagePointer != IntPtr.Zero)
                    {
                        Debug.WriteLine("Deserialized message update");
                        var id = Message.GetMessageIdFromPtr(updatedMessagePointer);
                        if (messageWrappers.TryGetValue(id, out var existingMessageWrapper))
                        {
                            existingMessageWrapper.UpdateWithPartialPtr(updatedMessagePointer);
                            existingMessageWrapper.BroadcastMessageUpdate();
                        }

                        pn_dispose_message(pointer);
                        continue;
                    }

                    //Updated channel
                    var channelPointer = pn_deserialize_channel(chatPointer, pointer);
                    if (channelPointer != IntPtr.Zero)
                    {
                        Debug.WriteLine("Deserialized channel update");

                        var id = Channel.GetChannelIdFromPtr(channelPointer);

                        //TODO: temporary get_channel update for ThreadChannels
                        if (id.Contains("PUBNUB_INTERNAL_THREAD"))
                        {
                            //This has a check for "PUBNUB_INTERNAL_THREAD" and will correctly update the pointer
                            TryGetChannel(id, out var existingThreadChannel);
                            //TODO: broadcast thread channel update (very low priority because I don't think they have that in JS chat)
                            existingThreadChannel.BroadcastChannelUpdate();
                        }
                        else if (channelWrappers.TryGetValue(id, out var existingChannelWrapper))
                        {
                            existingChannelWrapper.UpdateWithPartialPtr(channelPointer);
                            existingChannelWrapper.BroadcastChannelUpdate();
                        }

                        pn_dispose_message(pointer);
                        continue;
                    }

                    //Updated user
                    var userPointer = pn_deserialize_user(chatPointer, pointer);
                    if (userPointer != IntPtr.Zero)
                    {
                        Debug.WriteLine("Deserialized user update");

                        var id = User.GetUserIdFromPtr(userPointer);
                        if (userWrappers.TryGetValue(id, out var existingUserWrapper))
                        {
                            existingUserWrapper.UpdateWithPartialPtr(userPointer);
                            existingUserWrapper.BroadcastUserUpdate();
                        }

                        pn_dispose_message(pointer);
                        continue;
                    }

                    //Updated membership
                    var membershipPointer = pn_deserialize_membership(chatPointer, pointer);
                    if (membershipPointer != IntPtr.Zero)
                    {
                        Debug.WriteLine("Deserialized membership");

                        var id = Membership.GetMembershipIdFromPtr(membershipPointer);
                        if (membershipWrappers.TryGetValue(id, out var existingMembershipWrapper))
                        {
                            existingMembershipWrapper.UpdateWithPartialPtr(membershipPointer);
                            existingMembershipWrapper.BroadcastMembershipUpdate();
                        }

                        pn_dispose_message(pointer);
                        continue;
                    }

                    var presenceBuffer = new StringBuilder(16384);
                    //Presence (json list of uuids)
                    if (pn_deserialize_presence(pointer, presenceBuffer) != -1)
                    {
                        Debug.WriteLine("Deserialized presence update");

                        var channelId = presenceBuffer.ToString();
                        if (channelId.EndsWith("-pnpres"))
                        {
                            channelId = channelId.Substring(0,
                                channelId.LastIndexOf("-pnpres", StringComparison.Ordinal));
                        }

                        if (TryGetChannel(channelId, out var channel))
                        {
                            channel.BroadcastPresenceUpdate();
                        }

                        pn_dispose_message(pointer);
                        continue;
                    }

                    Debug.WriteLine("Wasn't able to deserialize incoming pointer into any known type!");
                    pn_dispose_message(pointer);
                }
            }
        }

        internal string GetUpdates()
        {
            var messagesBuffer = new StringBuilder(32768);
            CUtilities.CheckCFunctionResult(pn_chat_get_updates(chatPointer, messagesBuffer));
            return messagesBuffer.ToString();
        }

        #endregion

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

        public List<Channel> GetChannelSuggestions(string text, int limit = 10)
        {
            var buffer = new StringBuilder(2048);
            CUtilities.CheckCFunctionResult(pn_chat_get_channel_suggestions(chatPointer, text, limit, buffer));
            var suggestionsJson = buffer.ToString();
            var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, IntPtr[]>>(suggestionsJson);
            var channels = new List<Channel>();
            if (jsonDict == null || !jsonDict.TryGetValue("value", out var pointers) || pointers == null)
            {
                return channels;
            }

            channels = PointerParsers.ParseJsonChannelPointers(this, pointers);
            return channels;
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
        public Channel CreatePublicConversation(string channelId)
        {
            return CreatePublicConversation(channelId, new ChatChannelData());
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
        public Channel CreatePublicConversation(string channelId, ChatChannelData additionalData)
        {
            if (channelWrappers.TryGetValue(channelId, out var existingChannel))
            {
                Debug.WriteLine("Trying to create a channel with ID that already exists! Returning existing one.");
                return existingChannel;
            }

            var channelPointer = pn_chat_create_public_conversation_dirty(chatPointer, channelId,
                additionalData.ChannelName,
                additionalData.ChannelDescription,
                additionalData.ChannelCustomDataJson,
                additionalData.ChannelUpdated,
                additionalData.ChannelStatus,
                additionalData.ChannelType);
            CUtilities.CheckCFunctionResult(channelPointer);
            var channel = new Channel(this, channelId, channelPointer);
            channelWrappers.Add(channelId, channel);
            return channel;
        }

        public CreatedChannelWrapper CreateDirectConversation(User user, string channelId)
        {
            return CreateDirectConversation(user, channelId, new ChatChannelData());
        }

        public CreatedChannelWrapper CreateDirectConversation(User user, string channelId, ChatChannelData channelData)
        {
            var wrapperPointer = pn_chat_create_direct_conversation_dirty(chatPointer, user.Pointer, channelId,
                channelData.ChannelName,
                channelData.ChannelDescription, channelData.ChannelCustomDataJson, channelData.ChannelUpdated,
                channelData.ChannelStatus, channelData.ChannelType);
            CUtilities.CheckCFunctionResult(wrapperPointer);

            var createdChannelPointer = pn_chat_get_created_channel_wrapper_channel(wrapperPointer);
            CUtilities.CheckCFunctionResult(createdChannelPointer);
            TryGetChannel(createdChannelPointer, out var createdChannel);

            var hostMembershipPointer = pn_chat_get_created_channel_wrapper_host_membership(wrapperPointer);
            CUtilities.CheckCFunctionResult(hostMembershipPointer);
            TryGetMembership(hostMembershipPointer, out var hostMembership);

            var buffer = new StringBuilder(8192);
            CUtilities.CheckCFunctionResult(
                pn_chat_get_created_channel_wrapper_invited_memberships(wrapperPointer, buffer));
            var inviteeMembership = PointerParsers.ParseJsonMembershipPointers(this, buffer.ToString())[0];

            pn_chat_dispose_created_channel_wrapper(wrapperPointer);

            return new CreatedChannelWrapper()
            {
                CreatedChannel = createdChannel,
                HostMembership = hostMembership,
                InviteesMemberships = new List<Membership>() { inviteeMembership }
            };
        }

        public CreatedChannelWrapper CreateGroupConversation(List<User> users, string channelId)
        {
            return CreateGroupConversation(users, channelId, new ChatChannelData());
        }

        public CreatedChannelWrapper CreateGroupConversation(List<User> users, string channelId,
            ChatChannelData channelData)
        {
            var wrapperPointer = pn_chat_create_group_conversation_dirty(chatPointer,
                users.Select(x => x.Pointer).ToArray(), users.Count, channelId, channelData.ChannelName,
                channelData.ChannelDescription, channelData.ChannelCustomDataJson, channelData.ChannelUpdated,
                channelData.ChannelStatus, channelData.ChannelType);
            CUtilities.CheckCFunctionResult(wrapperPointer);

            var createdChannelPointer = pn_chat_get_created_channel_wrapper_channel(wrapperPointer);
            CUtilities.CheckCFunctionResult(createdChannelPointer);
            TryGetChannel(createdChannelPointer, out var createdChannel);

            var hostMembershipPointer = pn_chat_get_created_channel_wrapper_host_membership(wrapperPointer);
            CUtilities.CheckCFunctionResult(hostMembershipPointer);
            TryGetMembership(hostMembershipPointer, out var hostMembership);

            var buffer = new StringBuilder(8192);
            CUtilities.CheckCFunctionResult(
                pn_chat_get_created_channel_wrapper_invited_memberships(wrapperPointer, buffer));
            var inviteeMemberships = PointerParsers.ParseJsonMembershipPointers(this, buffer.ToString());

            pn_chat_dispose_created_channel_wrapper(wrapperPointer);

            return new CreatedChannelWrapper()
            {
                CreatedChannel = createdChannel,
                HostMembership = hostMembership,
                InviteesMemberships = inviteeMemberships
            };
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
        public bool TryGetChannel(string channelId, out Channel channel)
        {
            //Fetching and updating a ThreadChannel
            if (channelId.Contains("PUBNUB_INTERNAL_THREAD_"))
            {
                var channelAndTimeToken = channelId.Replace("PUBNUB_INTERNAL_THREAD_", "");
                var split = channelAndTimeToken.LastIndexOf("_", StringComparison.Ordinal);
                var parentChannelId = channelAndTimeToken.Remove(split);
                var messageTimeToken = channelAndTimeToken.Remove(0, split + 1);
                if (TryGetMessage(parentChannelId, messageTimeToken, out var message) &&
                    TryGetThreadChannel(message, out var threadChannel))
                {
                    channel = threadChannel;
                    return true;
                }
                else
                {
                    Debug.WriteLine("Didn't manage to find the ThreadChannel!");
                    channel = null;
                    return false;
                }
            }
            //Fetching and updating a regular channel
            else
            {
                var channelPointer = pn_chat_get_channel(chatPointer, channelId);
                return TryGetChannel(channelId, channelPointer, out channel);
            }
        }

        internal bool TryGetChannel(IntPtr channelPointer, out Channel channel)
        {
            var channelId = Channel.GetChannelIdFromPtr(channelPointer);
            return TryGetChannel(channelId, channelPointer, out channel);
        }

        internal bool TryGetChannel(string channelId, IntPtr channelPointer, out Channel channel)
        {
            return TryGetWrapper(channelWrappers, channelId, channelPointer,
                () => new Channel(this, channelId, channelPointer), out channel);
        }

        public ChannelsResponseWrapper GetChannels(string filter = "", string sort = "", int limit = 0,
            Page page = null)
        {
            var buffer = new StringBuilder(8192);
            page ??= new Page();
            CUtilities.CheckCFunctionResult(pn_chat_get_channels(chatPointer, filter, sort, limit, page.Next,
                page.Previous, buffer));
            var jsonChannelsWrapper = buffer.ToString();
            var internalChannelsWrapper =
                JsonConvert.DeserializeObject<InternalChannelsResponseWrapper>(jsonChannelsWrapper);
            return new ChannelsResponseWrapper(this, internalChannelsWrapper);
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
        public void UpdateChannel(string channelId, ChatChannelData updatedData)
        {
            var newPointer = pn_chat_update_channel_dirty(chatPointer, channelId, updatedData.ChannelName,
                updatedData.ChannelDescription,
                updatedData.ChannelCustomDataJson,
                updatedData.ChannelUpdated,
                updatedData.ChannelStatus,
                updatedData.ChannelType);
            CUtilities.CheckCFunctionResult(newPointer);
            if (channelWrappers.TryGetValue(channelId, out var existingChannelWrapper))
            {
                existingChannelWrapper.UpdatePointer(newPointer);
            }
            else
            {
                channelWrappers.Add(channelId, new Channel(this, channelId, newPointer));
            }
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
        public void DeleteChannel(string channelId)
        {
            if (channelWrappers.ContainsKey(channelId))
            {
                channelWrappers.Remove(channelId);
                CUtilities.CheckCFunctionResult(pn_chat_delete_channel(chatPointer, channelId));
            }
        }

        #endregion

        #region Users

        public bool TryGetCurrentUser(out User user)
        {
            var userPointer = pn_chat_current_user(chatPointer);
            CUtilities.CheckCFunctionResult(userPointer);
            return TryGetUser(userPointer, out user);
        }

        public List<User> GetUserSuggestions(string text, int limit = 10)
        {
            var buffer = new StringBuilder(2048);
            CUtilities.CheckCFunctionResult(pn_chat_get_user_suggestions(chatPointer, text, limit, buffer));
            var userPointersJson = buffer.ToString();
            var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, IntPtr[]>>(userPointersJson);
            if (jsonDict == null || !jsonDict.TryGetValue("value", out var pointers) || pointers == null)
            {
                return new List<User>();
            }

            return PointerParsers.ParseJsonUserPointers(this, pointers);
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
        public void SetRestriction(string userId, string channelId, bool banUser, bool muteUser, string reason)
        {
            CUtilities.CheckCFunctionResult(
                pn_chat_set_restrictions(chatPointer, userId, channelId, banUser, muteUser, reason));
        }

        public void SetRestriction(string userId, string channelId, Restriction restriction)
        {
            SetRestriction(userId, channelId, restriction.Ban, restriction.Mute, restriction.Reason);
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
        public User CreateUser(string userId)
        {
            return CreateUser(userId, new ChatUserData());
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
        public User CreateUser(string userId, ChatUserData additionalData)
        {
            if (userWrappers.TryGetValue(userId, out var existingUser))
            {
                Debug.WriteLine("Trying to create a user with ID that already exists! Returning existing one.");
                return existingUser;
            }

            var userPointer = pn_chat_create_user_dirty(chatPointer, userId,
                additionalData.Username,
                additionalData.ExternalId,
                additionalData.ProfileUrl,
                additionalData.Email,
                additionalData.CustomDataJson,
                additionalData.Status,
                additionalData.Status);
            CUtilities.CheckCFunctionResult(userPointer);
            var user = new User(this, userId, userPointer);
            userWrappers.Add(userId, user);
            return user;
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
        public bool IsPresent(string userId, string channelId)
        {
            if (TryGetChannel(channelId, out var channel))
            {
                return channel.IsUserPresent(userId);
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
        public List<string> WhoIsPresent(string channelId)
        {
            if (TryGetChannel(channelId, out var channel))
            {
                return channel.WhoIsPresent();
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
        public List<string> WherePresent(string userId)
        {
            if (TryGetUser(userId, out var user))
            {
                return user.WherePresent();
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
        public bool TryGetUser(string userId, out User user)
        {
            var userPointer = pn_chat_get_user(chatPointer, userId);
            return TryGetUser(userId, userPointer, out user);
        }

        internal bool TryGetUser(IntPtr userPointer, out User user)
        {
            var id = User.GetUserIdFromPtr(userPointer);
            return TryGetUser(id, userPointer, out user);
        }

        internal bool TryGetUser(string userId, IntPtr userPointer, out User user)
        {
            return TryGetWrapper(userWrappers, userId, userPointer, () => new User(this, userId, userPointer),
                out user);
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
        public UsersResponseWrapper GetUsers(string filter = "", string sort = "", int limit = 0, Page page = null)
        {
            var buffer = new StringBuilder(8192);
            page ??= new Page();
            CUtilities.CheckCFunctionResult(pn_chat_get_users(chatPointer, filter, sort, limit, page.Next,
                page.Previous, buffer));
            var internalWrapperJson = buffer.ToString();
            var internalWrapper = JsonConvert.DeserializeObject<InternalUsersResponseWrapper>(internalWrapperJson);
            return new UsersResponseWrapper(this, internalWrapper);
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
        public void UpdateUser(string userId, ChatUserData updatedData)
        {
            var newPointer = pn_chat_update_user_dirty(chatPointer, userId,
                updatedData.Username,
                updatedData.ExternalId,
                updatedData.ProfileUrl,
                updatedData.Email,
                updatedData.CustomDataJson,
                updatedData.Status,
                updatedData.Status);
            CUtilities.CheckCFunctionResult(newPointer);
            if (userWrappers.TryGetValue(userId, out var existingUserWrapper))
            {
                existingUserWrapper.UpdatePointer(newPointer);
            }
            //TODO: could and should this ever actually happen?
            else
            {
                userWrappers.Add(userId, new User(this, userId, newPointer));
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
        public void DeleteUser(string userId)
        {
            if (userWrappers.ContainsKey(userId))
            {
                userWrappers.Remove(userId);
                CUtilities.CheckCFunctionResult(pn_chat_delete_user(chatPointer, userId));
            }
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
        public MembersResponseWrapper GetUserMemberships(string userId, string filter = "", string sort = "",
            int limit = 0, Page page = null)
        {
            if (!TryGetUser(userId, out var user))
            {
                return new MembersResponseWrapper();
            }

            var buffer = new StringBuilder(8192);
            page ??= new Page();
            CUtilities.CheckCFunctionResult(pn_user_get_memberships(user.Pointer, filter, sort, limit, page.Next,
                page.Previous, buffer));
            var internalWrapperJson = buffer.ToString();
            var internalWrapper = JsonConvert.DeserializeObject<InternalMembersResponseWrapper>(internalWrapperJson);
            return new MembersResponseWrapper(this, internalWrapper);
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
        public MembersResponseWrapper GetChannelMemberships(string channelId, string filter = "", string sort = "",
            int limit = 0, Page page = null)
        {
            if (!TryGetChannel(channelId, out var channel))
            {
                return new MembersResponseWrapper();
            }

            page ??= new Page();
            var buffer = new StringBuilder(8192);
            CUtilities.CheckCFunctionResult(pn_channel_get_members(channel.Pointer, filter, sort, limit, page.Next,
                page.Previous, buffer));
            var internalWrapperJson = buffer.ToString();
            var internalWrapper = JsonConvert.DeserializeObject<InternalMembersResponseWrapper>(internalWrapperJson);
            return new MembersResponseWrapper(this, internalWrapper);
        }

        private bool TryGetMembership(IntPtr membershipPointer, out Membership membership)
        {
            var membershipId = Membership.GetMembershipIdFromPtr(membershipPointer);
            return TryGetMembership(membershipId, membershipPointer, out membership);
        }

        internal bool TryGetMembership(string membershipId, IntPtr membershipPointer, out Membership membership)
        {
            return TryGetWrapper(membershipWrappers, membershipId, membershipPointer,
                () => new Membership(this, membershipPointer, membershipId), out membership);
        }

        #endregion

        #region Messages

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
        public bool TryGetMessage(string channelId, string messageTimeToken, out Message message)
        {
            if (!TryGetChannel(channelId, out var channel))
            {
                message = null;
                return false;
            }

            var messagePointer = pn_channel_get_message(channel.Pointer, messageTimeToken);
            return TryGetMessage(messageTimeToken, messagePointer, out message);
        }

        public MarkMessagesAsReadWrapper MarkAllMessagesAsRead(string filter = "", string sort = "", int limit = 0,
            Page page = null)
        {
            page ??= new Page();
            var buffer = new StringBuilder(2048);
            CUtilities.CheckCFunctionResult(pn_chat_mark_all_messages_as_read(chatPointer, filter, sort, limit,
                page.Next, page.Previous, buffer));
            var internalWrapperJson = buffer.ToString();
            var internalWrapper = JsonConvert.DeserializeObject<InternalMarkMessagesAsReadWrapper>(internalWrapperJson);
            return new MarkMessagesAsReadWrapper(this, internalWrapper);
        }

        private bool TryGetMessage(string timeToken, IntPtr messagePointer, out Message message)
        {
            return TryGetWrapper(messageWrappers, timeToken, messagePointer,
                () => new Message(this, messagePointer, timeToken), out message);
        }

        internal bool TryGetMessage(IntPtr messagePointer, out Message message)
        {
            var messageId = Message.GetMessageIdFromPtr(messagePointer);
            return TryGetMessage(messageId, messagePointer, out message);
        }

        public List<UnreadMessageWrapper> GetUnreadMessagesCounts(string filter = "", string sort = "", int limit = 0,
            Page page = null)
        {
            var buffer = new StringBuilder(4096);
            page ??= new Page();
            CUtilities.CheckCFunctionResult(pn_chat_get_unread_messages_counts(chatPointer, filter, sort, limit,
                page.Next, page.Previous, buffer));
            var wrappersListJson = buffer.ToString();
            var internalWrappersList =
                JsonConvert.DeserializeObject<List<InternalUnreadMessageWrapper>>(wrappersListJson);
            var returnWrappers = new List<UnreadMessageWrapper>();
            if (internalWrappersList == null)
            {
                return returnWrappers;
            }

            foreach (var internalWrapper in internalWrappersList)
            {
                returnWrappers.Add(new UnreadMessageWrapper(this, internalWrapper));
            }

            return returnWrappers;
        }

        public ThreadChannel CreateThreadChannel(Message message)
        {
            if (TryGetThreadChannel(message, out var existingThread))
            {
                return existingThread;
            }

            var threadChannelPointer = pn_message_create_thread(message.Pointer);
            CUtilities.CheckCFunctionResult(threadChannelPointer);
            var newThread = new ThreadChannel(this, message, threadChannelPointer);
            channelWrappers.Add(newThread.Id, newThread);
            return newThread;
        }

        public void RemoveThreadChannel(Message message)
        {
            if (!TryGetThreadChannel(message, out var existingThread))
            {
                return;
            }

            CUtilities.CheckCFunctionResult(pn_message_remove_thread(message.Pointer));
            channelWrappers.Remove(existingThread.Id);
        }

        public bool TryGetThreadChannel(Message message, out ThreadChannel threadChannel)
        {
            var threadId = ThreadChannel.MessageToThreadChannelId(message);

            //No thread
            if (!message.HasThread())
            {
                channelWrappers.Remove(threadId);
                threadChannel = null;
                return false;
            }

            //Getting most up-to-date pointer
            var threadPointer = pn_message_get_thread(message.Pointer);

            //Existing thread pointer but not cached as a wrapper
            if (!channelWrappers.TryGetValue(threadId, out var existingChannel))
            {
                CUtilities.CheckCFunctionResult(threadPointer);
                threadChannel = new ThreadChannel(this, message, threadPointer);
                channelWrappers.Add(threadChannel.Id, threadChannel);
                return true;
            }

            //Existing wrapper
            if (existingChannel is ThreadChannel existingThreadChannel)
            {
                threadChannel = existingThreadChannel;
                threadChannel.UpdatePointer(threadPointer);
                return true;
            }
            else
            {
                throw new Exception("Chat wrapper error: cached ThreadChannel was of the wrong type!");
            }
        }

        public void ForwardMessage(Message message, Channel channel)
        {
            CUtilities.CheckCFunctionResult(pn_chat_forward_message(chatPointer, message.Pointer, channel.Pointer));
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

        public void PinMessageToChannel(string channelId, Message message)
        {
            if (TryGetChannel(channelId, out var channel))
            {
                channel.PinMessage(message);
            }
        }

        public void UnpinMessageFromChannel(string channelId)
        {
            if (TryGetChannel(channelId, out var channel))
            {
                channel.UnpinMessage();
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
        public List<Message> GetChannelMessageHistory(string channelId, string startTimeToken, string endTimeToken,
            int count)
        {
            var messages = new List<Message>();
            if (!TryGetChannel(channelId, out var channel))
            {
                Debug.WriteLine("Didn't find the channel for history fetch!");
                return messages;
            }

            var buffer = new StringBuilder(32768);
            CUtilities.CheckCFunctionResult(pn_channel_get_history(channel.Pointer, startTimeToken, endTimeToken, count,
                buffer));
            var jsonPointers = buffer.ToString();
            var messagePointers = JsonConvert.DeserializeObject<IntPtr[]>(jsonPointers);
            var returnMessages = new List<Message>();
            if (messagePointers == null)
            {
                return returnMessages;
            }

            foreach (var messagePointer in messagePointers)
            {
                var id = Message.GetMessageIdFromPtr(messagePointer);
                if (TryGetMessage(id, messagePointer, out var message))
                {
                    returnMessages.Add(message);
                }
            }

            return returnMessages;
        }

        #endregion

        #region Events

        public EventsHistoryWrapper GetEventsHistory(string channelId, string startTimeToken, string endTimeToken,
            int count)
        {
            var buffer = new StringBuilder(4096);
            CUtilities.CheckCFunctionResult(pn_chat_get_events_history(chatPointer, channelId, startTimeToken,
                endTimeToken, count, buffer));
            var wrapperJson = buffer.ToString();
            if (!CUtilities.IsValidJson(wrapperJson))
            {
                return new EventsHistoryWrapper();
                ;
            }

            return JsonConvert.DeserializeObject<EventsHistoryWrapper>(wrapperJson);
        }

        public void EmitEvent(PubnubChatEventType type, string channelId, string jsonPayload)
        {
            CUtilities.CheckCFunctionResult(pn_chat_emit_event(chatPointer, (byte)type, channelId, jsonPayload));
        }

        public void StartListeningForReportEvents(string channelId)
        {
            ListenForEvents($"PUBNUB_INTERNAL_MODERATION_{channelId}", PubnubChatEventType.Report);
        }

        public void StartListeningForCustomEvents(string channelId)
        {
            ListenForEvents(channelId, PubnubChatEventType.Custom);
        }

        public void StartListeningForMentionEvents(string userId)
        {
            ListenForEvents(userId, PubnubChatEventType.Mention);
        }

        public void StartListeningForInviteEvents(string userId)
        {
            ListenForEvents(userId, PubnubChatEventType.Invite);
        }

        public void StartListeningForModerationEvents(string userId)
        {
            ListenForEvents(userId, PubnubChatEventType.Moderation);
        }

        /// <summary>
        /// Starts listening for events.
        /// <para>
        /// Starts listening for channel events. 
        /// It allows to receive different events without the need to 
        /// connect to any channel.
        /// </para>
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <example>
        /// <code>
        /// var chat = // ...
        /// chat.ListenForEvents();
        /// chat.OnEvent += (event) => {
        ///  // Event received
        /// };
        /// </code>
        /// </example>
        /// <seealso cref="OnEvent"/>
        internal void ListenForEvents(string channelId, PubnubChatEventType type)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                throw new ArgumentException("Channel ID cannot be null or empty.");
            }

            var buffer = new StringBuilder(4096);
            CUtilities.CheckCFunctionResult(pn_chat_listen_for_events(chatPointer, channelId, (byte)type, buffer));
            ParseJsonUpdatePointers(buffer.ToString());
        }

        #endregion

        private bool TryGetWrapper<T>(Dictionary<string, T> wrappers, string id, IntPtr pointer, Func<T> createWrapper,
            out T wrapper) where T : ChatEntity
        {
            if (wrappers.TryGetValue(id, out wrapper))
            {
                //We had it before but it's no longer valid
                if (pointer == IntPtr.Zero)
                {
                    Debug.WriteLine(CUtilities.GetErrorMessage());
                    wrappers.Remove(id);
                    return false;
                }

                //Pointer is valid but something nulled the wrapper
                if (wrapper == null)
                {
                    wrappers[id] = createWrapper();
                    wrapper = wrappers[id];
                    return true;
                }
                //Updating existing wrapper with updated pointer
                else
                {
                    wrapper.UpdatePointer(pointer);
                    return true;
                }
            }
            //Adding new user to wrappers cache
            else if (pointer != IntPtr.Zero)
            {
                wrapper = createWrapper();
                wrappers.Add(id, wrapper);
                return true;
            }
            else
            {
                Debug.WriteLine(CUtilities.GetErrorMessage());
                return false;
            }
        }

        ~Chat()
        {
            fetchUpdates = false;
            fetchUpdatesThread.Join();
            pn_chat_delete(chatPointer);
        }
    }
}