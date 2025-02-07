using System;
using System.Threading.Tasks;

namespace PubNubChatAPI.Entities
{
    public abstract class ChatEntity
    {
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

        public abstract Task StartListeningForUpdates();
        
        public abstract Task StopListeningForUpdates();

        ~ChatEntity()
        {
            //StopListeningForUpdates();
            DisposePointer();
        }
    }
}