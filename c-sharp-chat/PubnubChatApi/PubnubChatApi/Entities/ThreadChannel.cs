using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public class ThreadChannel : Channel
    {
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern void pn_thread_channel_dispose(
            IntPtr thread_channel);

        [DllImport("pubnub-chat")]
        private static extern int pn_thread_channel_get_history(
            IntPtr thread_channel,
            string start_timetoken,
            string end_timetoken,
            int count,
            StringBuilder thread_messages_pointers_json);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_channel_pin_message_to_parent_channel(IntPtr thread_channel,
            IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_channel_unpin_message_from_parent_channel(IntPtr thread_channel);
        
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_channel_get_parent_channel_id(IntPtr thread_channel, StringBuilder result);
            
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_channel_parent_message(IntPtr thread_channel);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_channel_pin_message_to_thread(IntPtr thread_channel, IntPtr message);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_channel_unpin_message_from_thread(IntPtr thread_channel);

        [DllImport("pubnub-chat")]
        private static extern int pn_thread_channel_send_text(IntPtr thread_channel, string text);
        
        #endregion

        public string ParentChannelId
        {
            get
            {
                var buffer = new StringBuilder(128);
                CUtilities.CheckCFunctionResult(pn_thread_channel_get_parent_channel_id(pointer, buffer));
                return buffer.ToString();
            }
        }
        
        public Message ParentMessage
        {
            get
            {
                var parentMessagePointer = pn_thread_channel_parent_message(pointer);
                CUtilities.CheckCFunctionResult(parentMessagePointer);
                chat.TryGetMessage(parentMessagePointer, out var message);
                return message;
            }
        }
        
        internal static string MessageToThreadChannelId(Message message)
        {
            return $"PUBNUB_INTERNAL_THREAD_{message.ChannelId}_{message.Id}";
        }

        internal ThreadChannel(Chat chat, Message sourceMessage, IntPtr channelPointer) : base(chat,
            MessageToThreadChannelId(sourceMessage),
            channelPointer)
        {
        }

        public override async Task PinMessage(Message message)
        {
            var newPointer = await Task.Run(() => pn_thread_channel_pin_message_to_thread(pointer, message.Pointer));
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }

        public override async Task UnpinMessage()
        {
            var newPointer = await Task.Run(() => pn_thread_channel_unpin_message_from_thread(pointer));
            CUtilities.CheckCFunctionResult(newPointer);
            UpdatePointer(newPointer);
        }

        public async Task<List<ThreadMessage>> GetThreadHistory(string startTimeToken, string endTimeToken, int count)
        {
            Debug.WriteLine($"CHANNEL PARENT AT HISTORY: {ParentChannelId}");
            
            var buffer = new StringBuilder(4096);
            CUtilities.CheckCFunctionResult(await Task.Run(() => pn_thread_channel_get_history(pointer, startTimeToken, endTimeToken, count,
                buffer)));
            var messagesPointersJson = buffer.ToString();
            var history = new List<ThreadMessage>();
            if (!CUtilities.IsValidJson(messagesPointersJson))
            {
                return history;
            }

            var messagePointers = JsonConvert.DeserializeObject<IntPtr[]>(messagesPointersJson);
            if (messagePointers == null)
            {
                return history;
            }

            foreach (var threadMessagePointer in messagePointers)
            {
                var parentIdBuffer = new StringBuilder(128);
                CUtilities.CheckCFunctionResult(ThreadMessage.pn_thread_message_parent_channel_id(pointer, parentIdBuffer));
                Debug.WriteLine($"PARENT AT HISTORY: {parentIdBuffer.ToString()}");
                
                var id = ThreadMessage.GetThreadMessageIdFromPtr(threadMessagePointer);
                //This will also add a new wrapper if there wasn't one already
                if(chat.TryGetThreadMessage(id, threadMessagePointer, out var threadMessage))
                {
                    history.Add(threadMessage);
                }
                else
                {
                    Debug.WriteLine("Thread history messages aren't found/aren't thread messages - SHOULD BE IMPOSSIBLE!");
                }
            }
            return history;
        }

        public async Task PinMessageToParentChannel(Message message)
        {
            var newChannelPointer = await Task.Run(() => pn_thread_channel_pin_message_to_parent_channel(pointer, message.Pointer));
            CUtilities.CheckCFunctionResult(newChannelPointer);
            chat.UpdateChannelPointer(ParentChannelId, newChannelPointer);
        }

        public async Task UnPinMessageFromParentChannel()
        {
            var newChannelPointer = await Task.Run(() => pn_thread_channel_unpin_message_from_parent_channel(pointer));
            CUtilities.CheckCFunctionResult(newChannelPointer);
            chat.UpdateChannelPointer(ParentChannelId, newChannelPointer);
        }

        protected override void DisposePointer()
        {
            pn_thread_channel_dispose(pointer);
        }
    }
}