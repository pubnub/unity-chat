using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public abstract class ChatEntity
    {
        [DllImport("pubnub-chat")]
        protected static extern void pn_callback_handle_dispose(IntPtr handle);

        [DllImport("pubnub-chat")]
        protected static extern void pn_callback_handle_close(IntPtr handle);

        protected IntPtr updateListeningHandle;
        
        protected IntPtr pointer;
        internal IntPtr Pointer => pointer;
        
        internal ChatEntity(IntPtr pointer)
        {
            this.pointer = pointer;
        }

        internal void UpdatePointer(IntPtr newPointer)
        {
            DisposePointer();
            pointer = newPointer;
        }

        internal abstract void UpdateWithPartialPtr(IntPtr partialPointer);

        protected abstract void DisposePointer();

        protected abstract IntPtr StreamUpdates();

        public virtual async Task SetListeningForUpdates(bool listen)
        {
            updateListeningHandle = await SetListening(updateListeningHandle, listen, StreamUpdates);
        }

        protected async Task<IntPtr> SetListening(IntPtr callbackHandle, bool listen, Func<IntPtr> streamFunction)
        {
            if (listen)
            {
                if (callbackHandle != IntPtr.Zero)
                {
                    return callbackHandle;
                }
                callbackHandle = await Task.Run(streamFunction);
                CUtilities.CheckCFunctionResult(callbackHandle);
                return callbackHandle;
            }
            else
            {
                if (callbackHandle == IntPtr.Zero)
                {
                    return callbackHandle;
                }
                await Task.Run(() => {pn_callback_handle_close(callbackHandle);});
                if (callbackHandle != IntPtr.Zero)
                {
                    pn_callback_handle_dispose(callbackHandle);
                }
                callbackHandle = IntPtr.Zero;
                return callbackHandle;
            }
        }

        ~ChatEntity()
        {
            SetListeningForUpdates(false).Wait();
            DisposePointer();
        }
    }
}