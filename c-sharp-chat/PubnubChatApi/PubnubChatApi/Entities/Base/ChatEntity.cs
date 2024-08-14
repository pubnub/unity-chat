using System;

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

        public abstract void StartListeningForUpdates();
        
        //TODO: only after c-core event engine apparently
        //public abstract void StopListeningForUpdates();

        ~ChatEntity()
        {
            //StopListeningForUpdates();
            DisposePointer();
        }
    }
}