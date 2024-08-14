using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public class ThreadMessage : Message
    {
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern void pn_thread_message_dispose(
            IntPtr thread_message);

        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_get_timetoken(IntPtr thread_message, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_unpin_from_parent_channel(IntPtr thread_message);

        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_pin_to_parent_channel(IntPtr thread_message);
        
        [DllImport("pubnub-chat")]
        private static extern int pn_thread_message_parent_channel_id(IntPtr thread_message, StringBuilder result);
        
        [DllImport("pubnub-chat")]
        private static extern IntPtr pn_thread_message_update_with_base_message(IntPtr message, IntPtr base_message);

        #endregion
        
        public event Action<ThreadMessage> OnThreadMessageUpdated;

        public string ParentChannelId
        {
            get
            {
                var buffer = new StringBuilder(128);
                CUtilities.CheckCFunctionResult(pn_thread_message_parent_channel_id(pointer, buffer));
                return buffer.ToString();
            }
        }
        
        internal ThreadMessage(Chat chat, IntPtr messagePointer, string timeToken) : base(chat, messagePointer,
            timeToken)
        {
        }
        
        internal override void BroadcastMessageUpdate()
        {
            base.BroadcastMessageUpdate();
            Debug.WriteLine("NO JA KURWA PIERDOLĘ CO JEEEEEEEEST");
            Debug.WriteLine(Id);
            OnThreadMessageUpdated?.Invoke(this);
        }

        internal static string GetThreadMessageIdFromPtr(IntPtr threadMessagePointer)
        {
            var buffer = new StringBuilder(128);
            CUtilities.CheckCFunctionResult(pn_thread_message_get_timetoken(threadMessagePointer, buffer));
            return buffer.ToString();
        }

        internal override void UpdateWithPartialPtr(IntPtr partialPointer)
        {
            var newFullPointer = pn_thread_message_update_with_base_message(partialPointer, pointer);
            CUtilities.CheckCFunctionResult(newFullPointer);
            UpdatePointer(newFullPointer);
        }

        public void PinMessageToParentChannel()
        {
            var newChannelPointer = pn_thread_message_pin_to_parent_channel(pointer);
            CUtilities.CheckCFunctionResult(newChannelPointer);
            //TODO: this is to update the pointer of the existing wrapper, but isn't very explicit about the fact it does that
            chat.TryGetChannel(newChannelPointer, out _);
        }

        public void UnPinMessageFromParentChannel()
        {
            var newChannelPointer = pn_thread_message_unpin_from_parent_channel(pointer);
            CUtilities.CheckCFunctionResult(newChannelPointer);
            //TODO: this is to update the pointer of the existing wrapper, but isn't very explicit about the fact it does that
            chat.TryGetChannel(newChannelPointer, out _);
        }

        protected override void DisposePointer()
        {
            Debug.WriteLine($"On delete - ID is {Id}");
            pn_thread_message_dispose(pointer);
        }
    }
}