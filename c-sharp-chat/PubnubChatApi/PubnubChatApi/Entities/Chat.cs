using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_chat_new(
            string publish,
            string subscribe,
            string user_id,
            string auth_key,
            int typing_timeout,
            int typing_timeout_difference,
            int store_user_activity_interval,
            bool store_user_activity_timestamps);

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
        private static extern int pn_c_consume_response_buffer(IntPtr chat, StringBuilder result);

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
        private static extern IntPtr pn_chat_listen_for_events(
            IntPtr chat,
            string channel_id,
            byte event_type);

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
        private static extern int pn_chat_get_current_user_mentions(IntPtr chat, string start_timetoken,
            string end_timetoken, int count, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr
            pn_chat_create_direct_conversation_dirty_with_membership_data(
                IntPtr chat,
                IntPtr user,
                string channel_id,
                string channel_name,
                string channel_description,
                string channel_custom_data_json,
                string channel_updated,
                string channel_status,
                string channel_type,
                string membership_custom_json,
                string membership_type,
                string membership_status
            );

        [DllImport("pubnub-chat")]
        private static extern IntPtr
            pn_chat_create_group_conversation_dirty_with_membership_data(
                IntPtr chat,
                IntPtr[] users,
                int users_length,
                string channel_id,
                string channel_name,
                string channel_description,
                string channel_custom_data_json,
                string channel_updated,
                string channel_status,
                string channel_type,
                string membership_custom_json,
                string membership_type,
                string membership_status
            );

        #endregion

        private IntPtr chatPointer;
        internal IntPtr Pointer => chatPointer;
        private Dictionary<string, Channel> channelWrappers = new();
        private Dictionary<string, User> userWrappers = new();
        private Dictionary<string, Membership> membershipWrappers = new();
        private Dictionary<string, Message> messageWrappers = new();
        private bool fetchUpdates = true;

        public event Action<ChatEvent> OnAnyEvent;

        public ChatAccessManager ChatAccessManager { get; }
        public PubnubChatConfig Config { get; }

        /// <summary>
        /// Asynchronously initializes a new instance of the <see cref="Chat"/> class.
        /// <para>
        /// Creates a new chat instance.
        /// </para>
        /// </summary>
        /// <param name="config">Config with PubNub keys and values</param>
        /// <remarks>
        /// The constructor initializes the chat instance with the provided keys and user ID from the Config.
        /// </remarks>
        public static async Task<Chat> CreateInstance(PubnubChatConfig config)
        {
            var chat = await Task.Run(() => new Chat(config));
            chat.FetchUpdatesLoop();
            return chat;
        }

        internal Chat(PubnubChatConfig config)
        {
            chatPointer = pn_chat_new(config.PublishKey, config.SubscribeKey, config.UserId, config.AuthKey,
                config.TypingTimeout, config.TypingTimeoutDifference, config.StoreUserActivityInterval,
                config.StoreUserActivityTimestamp);
            CUtilities.CheckCFunctionResult(chatPointer);

            Config = config;
            ChatAccessManager = new ChatAccessManager(chatPointer);
        }

        #region Updates handling

        //TODO: cancellation token?
        internal async Task FetchUpdatesLoop()
        {
            while (fetchUpdates)
            {
                var updates = GetUpdates();
                try
                {
                    ParseJsonUpdatePointers(updates);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error when parsing JSON updates: {e}");
                }

                await Task.Delay(200);
            }
        }

        internal void ParseJsonUpdatePointers(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString) || jsonString == "[]")
            {
                return;
            }

            Debug.WriteLine($"Received JSON to parse: {jsonString}");

            var jArray = JArray.Parse(jsonString);

            var updates = jArray
                .Children<JObject>()
                .SelectMany(jo => jo.Properties())
                .GroupBy(jp => jp.Name)
                .ToDictionary(
                    grp => grp.Key,
                    grp => grp.SelectMany(jp =>
                        jp.Value is JArray ? jp.Value.Values<string>() : new[] { jp.Value.ToString() }).ToList()
                );

            foreach (var update in updates)
            {
                foreach (var json in update.Value)
                {
                    if (json == null)
                    {
                        continue;
                    }

                    Debug.WriteLine($"Parsing JSON:\n--Key: {update.Key},\n--Value: {json}");

                    switch (update.Key)
                    {
                        // {"channel_id": "<channel_name>", "data" : [{"<timetoken>": ["<user1>", "<user2>"]}]}
                        case "read_receipts":
                            var jObject = JObject.Parse(json);
                            if (!jObject.TryGetValue("channel_id", out var readChannelId)
                                || !jObject.TryGetValue("data", out var data))
                            {
                                Debug.WriteLine("Incorrect read recepits JSON payload!");
                                continue;
                            }

                            if (!TryGetChannel(readChannelId.ToString(), out var readReceiptChannel))
                            {
                                Debug.WriteLine("Can't find the read receipt channel!");
                                continue;
                            }

                            var receipts = data.Children()
                                .SelectMany(j => j.Children<JProperty>())
                                .ToDictionary(jp => jp.Name, jp => jp.Value.ToObject<List<string>>());
                            readReceiptChannel.BroadcastReadReceipt(receipts);
                            OnAnyEvent?.Invoke(new ChatEvent()
                            {
                                ChannelId = readChannelId.ToString(),
                                Type = PubnubChatEventType.Receipt,
                                Payload = json
                            });
                            break;
                        case "typing_users":
                            var typings = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
                            if (typings == null)
                            {
                                continue;
                            }

                            foreach (var kvp in typings)
                            {
                                if (TryGetChannel(kvp.Key, out var typingChannel))
                                {
                                    typingChannel.TryParseAndBroadcastTypingEvent(kvp.Value);
                                    OnAnyEvent?.Invoke(new ChatEvent()
                                    {
                                        ChannelId = kvp.Key,
                                        Payload = json,
                                        Type = PubnubChatEventType.Typing
                                    });
                                }
                            }

                            break;
                        case "event":
                        case "message_report":
                            Debug.WriteLine("Deserialized event / message report");

                            if (!CUtilities.IsValidJson(json))
                            {
                                break;
                            }

                            var chatEvent = JsonConvert.DeserializeObject<ChatEvent>(json);
                            var invoked = false;
                            //TODO: not a big fan of this big-ass switch
                            switch (chatEvent.Type)
                            {
                                case PubnubChatEventType.Report:
                                    var moderationPrefix = "PUBNUB_INTERNAL_MODERATION_";
                                    var index = chatEvent.ChannelId.IndexOf(moderationPrefix, StringComparison.Ordinal);
                                    var properChannelId = (index < 0)
                                        ? chatEvent.ChannelId
                                        : chatEvent.ChannelId.Remove(index, moderationPrefix.Length);
                                    if (TryGetChannel(properChannelId, out var reportChannel))
                                    {
                                        reportChannel.BroadcastReportEvent(chatEvent);
                                        invoked = true;
                                    }

                                    break;
                                case PubnubChatEventType.Mention:
                                    if (TryGetUser(chatEvent.UserId, out var mentionedUser))
                                    {
                                        mentionedUser.BroadcastMentionEvent(chatEvent);
                                        invoked = true;
                                    }

                                    break;
                                case PubnubChatEventType.Invite:
                                    if (TryGetUser(chatEvent.UserId, out var invitedUser))
                                    {
                                        invitedUser.BroadcastInviteEvent(chatEvent);
                                        invoked = true;
                                    }

                                    break;
                                case PubnubChatEventType.Custom:
                                    if (TryGetChannel(chatEvent.ChannelId, out var customEventChannel))
                                    {
                                        customEventChannel.BroadcastCustomEvent(chatEvent);
                                        invoked = true;
                                    }

                                    break;
                                case PubnubChatEventType.Moderation:
                                    if (TryGetUser(chatEvent.UserId, out var moderatedUser))
                                    {
                                        moderatedUser.BroadcastModerationEvent(chatEvent);
                                        invoked = true;
                                    }

                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            if (invoked)
                            {
                                OnAnyEvent?.Invoke(chatEvent);
                            }

                            break;
                        case "message":
                            var messagePointer = JsonConvert.DeserializeObject<IntPtr>(json);
                            if (messagePointer != IntPtr.Zero)
                            {
                                Debug.WriteLine("Deserialized new message");

                                var id = Message.GetChannelIdFromMessagePtr(messagePointer);
                                if (channelWrappers.TryGetValue(id, out var channel))
                                {
                                    var timeToken = Message.GetMessageIdFromPtr(messagePointer);
                                    var message = new Message(this, messagePointer, timeToken);
                                    //We don't store ThreadMessage wrappers by default, only add them if
                                    //specifically requested/created in TryGetThreadHistory
                                    if (!id.Contains("PUBNUB_INTERNAL_THREAD_"))
                                    {
                                        messageWrappers[timeToken] = message;
                                    }

                                    channel.BroadcastMessageReceived(message);
                                }
                            }

                            break;
                        case "thread_message_update":
                            var updatedThreadMessagePointer = JsonConvert.DeserializeObject<IntPtr>(json);
                            if (updatedThreadMessagePointer != IntPtr.Zero)
                            {
                                Debug.WriteLine("Deserialized thread message update");
                                var id = ThreadMessage.GetThreadMessageIdFromPtr(updatedThreadMessagePointer);
                                if (messageWrappers.TryGetValue(id, out var existingMessageWrapper))
                                {
                                    if (existingMessageWrapper is ThreadMessage existingThreadMessageWrapper)
                                    {
                                        existingThreadMessageWrapper.UpdateWithPartialPtr(updatedThreadMessagePointer);
                                        existingThreadMessageWrapper.BroadcastMessageUpdate();
                                    }
                                    else
                                    {
                                        Debug.WriteLine(
                                            "Thread message was stored as a regular message - SHOULD NEVER HAPPEN!");
                                    }
                                }
                            }

                            break;
                        case "message_update":
                            var updatedMessagePointer = JsonConvert.DeserializeObject<IntPtr>(json);
                            if (updatedMessagePointer != IntPtr.Zero)
                            {
                                Debug.WriteLine("Deserialized message update");
                                var id = Message.GetMessageIdFromPtr(updatedMessagePointer);
                                if (messageWrappers.TryGetValue(id, out var existingMessageWrapper))
                                {
                                    existingMessageWrapper.UpdateWithPartialPtr(updatedMessagePointer);
                                    existingMessageWrapper.BroadcastMessageUpdate();
                                }
                            }

                            break;
                        case "channel_update":
                            var channelPointer = JsonConvert.DeserializeObject<IntPtr>(json);
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
                            }

                            break;
                        case "user_update":
                            var userPointer = JsonConvert.DeserializeObject<IntPtr>(json);
                            if (userPointer != IntPtr.Zero)
                            {
                                Debug.WriteLine("Deserialized user update");

                                var id = User.GetUserIdFromPtr(userPointer);
                                if (userWrappers.TryGetValue(id, out var existingUserWrapper))
                                {
                                    existingUserWrapper.UpdateWithPartialPtr(userPointer);
                                    existingUserWrapper.BroadcastUserUpdate();
                                }
                            }

                            break;
                        case "membership_update":
                            var membershipPointer = JsonConvert.DeserializeObject<IntPtr>(json);
                            if (membershipPointer != IntPtr.Zero)
                            {
                                Debug.WriteLine("Deserialized membership");

                                var id = Membership.GetMembershipIdFromPtr(membershipPointer);
                                if (membershipWrappers.TryGetValue(id, out var existingMembershipWrapper))
                                {
                                    existingMembershipWrapper.UpdateWithPartialPtr(membershipPointer);
                                    existingMembershipWrapper.BroadcastMembershipUpdate();
                                }
                            }

                            break;
                        case "presence_users":
                            Debug.WriteLine("Deserialized presence update");
                            var presenceDictionary =
                                JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
                            if (presenceDictionary == null)
                            {
                                break;
                            }

                            foreach (var pair in presenceDictionary)
                            {
                                var channelId = pair.Key;
                                if (channelId.EndsWith("-pnpres"))
                                {
                                    channelId = channelId.Substring(0,
                                        channelId.LastIndexOf("-pnpres", StringComparison.Ordinal));
                                }

                                if (TryGetChannel(channelId, out var channel))
                                {
                                    channel.BroadcastPresenceUpdate();
                                }
                            }

                            break;
                        default:
                            Debug.WriteLine("Wasn't able to deserialize incoming pointer into any known type!");
                            break;
                    }
                }
            }
        }

        internal string GetUpdates()
        {
            var messagesBuffer = new StringBuilder(32768);
            CUtilities.CheckCFunctionResult(pn_c_consume_response_buffer(chatPointer, messagesBuffer));
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
        public async Task<Channel> CreatePublicConversation(string channelId = "")
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
        public async Task<Channel> CreatePublicConversation(string channelId, ChatChannelData additionalData)
        {
            if (channelWrappers.TryGetValue(channelId, out var existingChannel))
            {
                Debug.WriteLine("Trying to create a channel with ID that already exists! Returning existing one.");
                return existingChannel;
            }

            var channelPointer = await Task.Run(() => pn_chat_create_public_conversation_dirty(chatPointer, channelId,
                additionalData.ChannelName,
                additionalData.ChannelDescription,
                additionalData.ChannelCustomDataJson,
                additionalData.ChannelUpdated,
                additionalData.ChannelStatus,
                additionalData.ChannelType));
            CUtilities.CheckCFunctionResult(channelPointer);
            var channel = new Channel(this, channelId, channelPointer);
            channelWrappers.Add(channelId, channel);
            return channel;
        }

        public async Task<CreatedChannelWrapper> CreateDirectConversation(User user, string channelId = "",
            ChatChannelData? channelData = null, ChatMembershipData? membershipData = null)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                channelId = Guid.NewGuid().ToString();
            }

            channelData ??= new ChatChannelData();

            IntPtr wrapperPointer;

            if (membershipData == null)
            {
                wrapperPointer = await Task.Run(() => pn_chat_create_direct_conversation_dirty(chatPointer,
                    user.Pointer, channelId,
                    channelData.ChannelName,
                    channelData.ChannelDescription, channelData.ChannelCustomDataJson, channelData.ChannelUpdated,
                    channelData.ChannelStatus, channelData.ChannelType));
            }
            else
            {
                wrapperPointer = await Task.Run(() => pn_chat_create_direct_conversation_dirty_with_membership_data(
                    chatPointer,
                    user.Pointer, channelId,
                    channelData.ChannelName,
                    channelData.ChannelDescription, channelData.ChannelCustomDataJson, channelData.ChannelUpdated,
                    channelData.ChannelStatus, channelData.ChannelType, membershipData.CustomDataJson,
                    membershipData.Type, membershipData.Status));
            }

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

        public async Task<CreatedChannelWrapper> CreateGroupConversation(List<User> users, string channelId = "",
            ChatChannelData? channelData = null, ChatMembershipData? membershipData = null)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                channelId = Guid.NewGuid().ToString();
            }
            channelData ??= new ChatChannelData();

            IntPtr wrapperPointer;
            if (membershipData == null)
            {
                wrapperPointer = await Task.Run(() => pn_chat_create_group_conversation_dirty(chatPointer,
                    users.Select(x => x.Pointer).ToArray(), users.Count, channelId, channelData.ChannelName,
                    channelData.ChannelDescription, channelData.ChannelCustomDataJson, channelData.ChannelUpdated,
                    channelData.ChannelStatus, channelData.ChannelType));
            }
            else
            {
                wrapperPointer = await Task.Run(() => pn_chat_create_group_conversation_dirty_with_membership_data(
                    chatPointer,
                    users.Select(x => x.Pointer).ToArray(), users.Count, channelId, channelData.ChannelName,
                    channelData.ChannelDescription, channelData.ChannelCustomDataJson, channelData.ChannelUpdated,
                    channelData.ChannelStatus, channelData.ChannelType, membershipData.CustomDataJson,
                    membershipData.Type, membershipData.Status));
            }

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
        /// <seealso cref="GetChannelAsync"/>
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

        /// <summary>
        /// Performs an async retrieval of a Channel object with a given ID.
        /// </summary>
        /// <param name="channelId">ID of the channel.</param>
        /// <returns>Channel object if it exists, null otherwise.</returns>
        public async Task<Channel?> GetChannelAsync(string channelId)
        {
            return await Task.Run(() =>
            {
                var result = TryGetChannel(channelId, out var channel);
                return result ? channel : null;
            });
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

        //The TryGetChannel updates the pointer, these methods are for internal logic explicity sake
        internal void UpdateChannelPointer(IntPtr newPointer)
        {
            TryGetChannel(newPointer, out _);
        }

        internal void UpdateChannelPointer(string id, IntPtr newPointer)
        {
            TryGetChannel(id, newPointer, out _);
        }

        public async Task<ChannelsResponseWrapper> GetChannels(string filter = "", string sort = "", int limit = 0,
            Page page = null)
        {
            var buffer = new StringBuilder(8192);
            page ??= new Page();
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_chat_get_channels(chatPointer, filter, sort, limit,
                page.Next,
                page.Previous, buffer)));
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
        public async Task UpdateChannel(string channelId, ChatChannelData updatedData)
        {
            var newPointer = await Task.Run(() => pn_chat_update_channel_dirty(chatPointer, channelId,
                updatedData.ChannelName,
                updatedData.ChannelDescription,
                updatedData.ChannelCustomDataJson,
                updatedData.ChannelUpdated,
                updatedData.ChannelStatus,
                updatedData.ChannelType));
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
        public async Task DeleteChannel(string channelId)
        {
            if (channelWrappers.ContainsKey(channelId))
            {
                channelWrappers.Remove(channelId);
                CUtilities.CheckCFunctionResult(await Task.Run(() => pn_chat_delete_channel(chatPointer, channelId)));
            }
        }

        #endregion

        #region Users

        public async Task<UserMentionsWrapper> GetCurrentUserMentions(string startTimeToken, string endTimeToken,
            int count)
        {
            var buffer = new StringBuilder(4096);
            CUtilities.CheckCFunctionResult(await Task.Run(() =>
                pn_chat_get_current_user_mentions(chatPointer, startTimeToken, endTimeToken, count, buffer)));
            var internalWrapperJson = buffer.ToString();
            var emptyResponse = new UserMentionsWrapper(this, new InternalUserMentionsWrapper());
            if (!CUtilities.IsValidJson(internalWrapperJson))
            {
                return emptyResponse;
            }

            var internalWrapper = JsonConvert.DeserializeObject<InternalUserMentionsWrapper>(internalWrapperJson);
            if (internalWrapper == null)
            {
                return emptyResponse;
            }

            return new UserMentionsWrapper(this, internalWrapper);
        }

        /// <summary>
        /// Tries to retrieve the current User object for this chat.
        /// </summary>
        /// <param name="user">The retrieved current User object.</param>
        /// <returns>True if chat has a current user, false otherwise.</returns>
        /// <seealso cref="GetCurrentUserAsync"/>
        public bool TryGetCurrentUser(out User user)
        {
            var userPointer = pn_chat_current_user(chatPointer);
            CUtilities.CheckCFunctionResult(userPointer);
            return TryGetUser(userPointer, out user);
        }

        /// <summary>
        /// Asynchronously tries to retrieve the current User object for this chat.
        /// </summary>
        /// <returns>User object if there is a current user, null otherwise.</returns>
        public async Task<User?> GetCurrentUserAsync()
        {
            return await Task.Run(() =>
            {
                var result = TryGetCurrentUser(out var currentUser);
                return result ? currentUser : null;
            });
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
            CUtilities.CheckCFunctionResult(
                await Task.Run(
                    () => pn_chat_set_restrictions(chatPointer, userId, channelId, banUser, muteUser, reason)));
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
        public async Task<User> CreateUser(string userId)
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
        public async Task<User> CreateUser(string userId, ChatUserData additionalData)
        {
            if (userWrappers.TryGetValue(userId, out var existingUser))
            {
                Debug.WriteLine("Trying to create a user with ID that already exists! Returning existing one.");
                return existingUser;
            }

            var userPointer = await Task.Run(() => pn_chat_create_user_dirty(chatPointer, userId,
                additionalData.Username,
                additionalData.ExternalId,
                additionalData.ProfileUrl,
                additionalData.Email,
                additionalData.CustomDataJson,
                additionalData.Status,
                additionalData.Status));
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
            var userPointer = pn_chat_get_user(chatPointer, userId);
            return TryGetUser(userId, userPointer, out user);
        }

        /// <summary>
        /// Asynchronously gets the user with the provided user ID.
        /// </summary>
        /// <param name="userId">ID of the User to get.</param>
        /// <returns>User object if one with given ID is found, null otherwise.</returns>
        public async Task<User?> GetUserAsync(string userId)
        {
            return await Task.Run(() =>
            {
                var result = TryGetUser(userId, out var user);
                return result ? user : null;
            });
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
        public async Task<UsersResponseWrapper> GetUsers(string filter = "", string sort = "", int limit = 0,
            Page page = null)
        {
            var buffer = new StringBuilder(8192);
            page ??= new Page();
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_chat_get_users(chatPointer, filter, sort, limit,
                page.Next,
                page.Previous, buffer)));
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
        public async Task UpdateUser(string userId, ChatUserData updatedData)
        {
            var newPointer = await Task.Run(() => pn_chat_update_user_dirty(chatPointer, userId,
                updatedData.Username,
                updatedData.ExternalId,
                updatedData.ProfileUrl,
                updatedData.Email,
                updatedData.CustomDataJson,
                updatedData.Status,
                updatedData.Type));
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
        public async Task DeleteUser(string userId)
        {
            if (userWrappers.ContainsKey(userId))
            {
                userWrappers.Remove(userId);
                CUtilities.CheckCFunctionResult(await Task.Run(() => pn_chat_delete_user(chatPointer, userId)));
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
        public async Task<MembersResponseWrapper> GetUserMemberships(string userId, string filter = "",
            string sort = "",
            int limit = 0, Page page = null)
        {
            if (!TryGetUser(userId, out var user))
            {
                return new MembersResponseWrapper();
            }

            var buffer = new StringBuilder(8192);
            page ??= new Page();
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_user_get_memberships(user.Pointer, filter, sort,
                limit, page.Next,
                page.Previous, buffer)));
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
        public async Task<MembersResponseWrapper> GetChannelMemberships(string channelId, string filter = "",
            string sort = "",
            int limit = 0, Page page = null)
        {
            if (!TryGetChannel(channelId, out var channel))
            {
                return new MembersResponseWrapper();
            }

            page ??= new Page();
            var buffer = new StringBuilder(8192);
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_channel_get_members(channel.Pointer, filter, sort,
                limit, page.Next,
                page.Previous, buffer)));
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
            if (!TryGetChannel(channelId, out var channel))
            {
                message = null;
                return false;
            }

            var messagePointer = pn_channel_get_message(channel.Pointer, messageTimeToken);
            return TryGetMessage(messageTimeToken, messagePointer, out message);
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
            page ??= new Page();
            var buffer = new StringBuilder(2048);
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_chat_mark_all_messages_as_read(chatPointer, filter,
                sort, limit,
                page.Next, page.Previous, buffer)));
            var internalWrapperJson = buffer.ToString();
            var internalWrapper = JsonConvert.DeserializeObject<InternalMarkMessagesAsReadWrapper>(internalWrapperJson);
            return new MarkMessagesAsReadWrapper(this, internalWrapper);
        }

        private bool TryGetMessage(string timeToken, IntPtr messagePointer, out Message message)
        {
            return TryGetWrapper(messageWrappers, timeToken, messagePointer,
                () => new Message(this, messagePointer, timeToken), out message);
        }

        internal bool TryGetAnyMessage(string timeToken, out Message message)
        {
            return messageWrappers.TryGetValue(timeToken, out message);
        }

        internal bool TryGetThreadMessage(string timeToken, IntPtr threadMessagePointer,
            out ThreadMessage threadMessage)
        {
            var found = TryGetWrapper(messageWrappers, timeToken, threadMessagePointer,
                () => new ThreadMessage(this, threadMessagePointer, timeToken), out var foundMessage);
            if (!found || foundMessage is not ThreadMessage foundThreadMessage)
            {
                threadMessage = null;
                return false;
            }
            else
            {
                threadMessage = foundThreadMessage;
                return true;
            }
        }

        internal bool TryGetMessage(IntPtr messagePointer, out Message message)
        {
            var messageId = Message.GetMessageIdFromPtr(messagePointer);
            return TryGetMessage(messageId, messagePointer, out message);
        }

        public async Task<List<UnreadMessageWrapper>> GetUnreadMessagesCounts(string filter = "", string sort = "",
            int limit = 0,
            Page page = null)
        {
            var buffer = new StringBuilder(4096);
            page ??= new Page();
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_chat_get_unread_messages_counts(chatPointer, filter,
                sort, limit,
                page.Next, page.Previous, buffer)));
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

        public async Task<ThreadChannel> CreateThreadChannel(Message message)
        {
            if (TryGetThreadChannel(message, out var existingThread))
            {
                return existingThread;
            }

            var threadChannelPointer = await Task.Run(() => pn_message_create_thread(message.Pointer));
            CUtilities.CheckCFunctionResult(threadChannelPointer);
            var newThread = new ThreadChannel(this, message, threadChannelPointer);
            channelWrappers.Add(newThread.Id, newThread);
            return newThread;
        }

        public async Task RemoveThreadChannel(Message message)
        {
            if (!TryGetThreadChannel(message, out var existingThread))
            {
                return;
            }

            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_message_remove_thread(message.Pointer)));
            channelWrappers.Remove(existingThread.Id);
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
            var threadId = ThreadChannel.MessageToThreadChannelId(message);

            //Getting most up-to-date pointer
            var threadPointer = pn_message_get_thread(message.Pointer);

            if (threadPointer == IntPtr.Zero)
            {
                channelWrappers.Remove(threadId);
                threadChannel = null;
                return false;
            }

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
            CUtilities.CheckCFunctionResult(await Task.Run(() =>
                pn_chat_forward_message(chatPointer, message.Pointer, channel.Pointer)));
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
            var messages = new List<Message>();
            if (!TryGetChannel(channelId, out var channel))
            {
                Debug.WriteLine("Didn't find the channel for history fetch!");
                return messages;
            }

            var buffer = new StringBuilder(32768);
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_channel_get_history(channel.Pointer, startTimeToken,
                endTimeToken, count,
                buffer)));
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

        public async Task<EventsHistoryWrapper> GetEventsHistory(string channelId, string startTimeToken,
            string endTimeToken,
            int count)
        {
            var buffer = new StringBuilder(4096);
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_chat_get_events_history(chatPointer, channelId,
                startTimeToken,
                endTimeToken, count, buffer)));
            var wrapperJson = buffer.ToString();
            if (!CUtilities.IsValidJson(wrapperJson))
            {
                return new EventsHistoryWrapper();
                ;
            }

            return JsonConvert.DeserializeObject<EventsHistoryWrapper>(wrapperJson);
        }

        public async Task EmitEvent(PubnubChatEventType type, string channelId, string jsonPayload)
        {
            CUtilities.CheckCFunctionResult(await Task.Run(() =>
                pn_chat_emit_event(chatPointer, (byte)type, channelId, jsonPayload)));
        }

        internal IntPtr ListenForEvents(string channelId, PubnubChatEventType type)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                throw new ArgumentException("Channel ID cannot be null or empty.");
            }

            var callbackHandler = pn_chat_listen_for_events(chatPointer, channelId, (byte)type);
            CUtilities.CheckCFunctionResult(callbackHandler);
            return callbackHandler;
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

        public void Destroy()
        {
            //TODO: a temporary solution, maybe nulling the ptr later will be better
            if (fetchUpdates == false)
            {
                return;
            }

            fetchUpdates = false;
            pn_chat_delete(chatPointer);
        }

        ~Chat()
        {
            Destroy();
        }
    }
}