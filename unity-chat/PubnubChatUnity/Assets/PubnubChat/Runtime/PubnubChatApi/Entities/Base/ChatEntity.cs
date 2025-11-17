using System;
using System.Threading.Tasks;
using PubnubApi;

namespace PubnubChatApi
{
    public abstract class ChatEntity
    {
        protected Chat chat;
        protected Subscription updateSubscription;
        protected abstract string UpdateChannelId { get; }

        internal ChatEntity(Chat chat)
        {
            this.chat = chat;
        }

        protected void SetListening(ref Subscription subscription, SubscriptionOptions subscriptionOptions, bool listen, string channelId, SubscribeCallback listener)
        {
            if (listen)
            {
                if (subscription != null)
                {
                    return;
                }
                subscription = chat.PubnubInstance.Channel(channelId).Subscription(subscriptionOptions);
                subscription.AddListener(listener);
                subscription.Subscribe<object>();
            }
            else
            {
                subscription?.Unsubscribe<object>();
            }
        }
        
        [Obsolete("Obsolete, please use StreamUpdates() instead")]
        public void SetListeningForUpdates(bool listen)
        {
            StreamUpdates(listen);
        }
        
        /// <summary>
        /// Sets whether to listen for update events on this entity.
        /// </summary>
        /// <param name="stream">True to start listening, false to stop listening.</param>
        public void StreamUpdates(bool stream)
        {
            SetListening(ref updateSubscription, SubscriptionOptions.None, stream, UpdateChannelId, CreateUpdateListener());
        }
        
        protected abstract SubscribeCallback CreateUpdateListener();
        
        public abstract Task<ChatOperationResult> Refresh();
    }
}