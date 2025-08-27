using System.Threading.Tasks;
using PubnubApi;

namespace PubNubChatAPI.Entities
{
    public abstract class ChatEntity
    {
        protected Chat chat;
        protected Subscription? updateSubscription;
        protected abstract string UpdateChannelId { get; }

        internal ChatEntity(Chat chat)
        {
            this.chat = chat;
        }

        protected void SetListening(Subscription subscription, SubscriptionOptions subscriptionOptions, bool listen, string channelId, SubscribeCallback listener)
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
        
        public virtual void SetListeningForUpdates(bool listen)
        {
            SetListening(updateSubscription, SubscriptionOptions.None, listen, UpdateChannelId, CreateUpdateListener());
        }
        
        protected abstract SubscribeCallback CreateUpdateListener();
        
        public abstract Task Refresh();
    }
}