using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public abstract class ChatEntity
    {
        protected Subscription? updateSubscription;
        
        public virtual void SetListeningForUpdates(bool listen)
        {
            throw new NotImplementedException();
        }
        
        public abstract Task Resync();
    }
}