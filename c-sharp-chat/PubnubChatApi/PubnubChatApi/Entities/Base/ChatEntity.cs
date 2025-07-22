using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public abstract class ChatEntity
    {
        public abstract Task Resync();

        public virtual void SetListeningForUpdates(bool listen)
        {
            throw new NotImplementedException();
        }
    }
}