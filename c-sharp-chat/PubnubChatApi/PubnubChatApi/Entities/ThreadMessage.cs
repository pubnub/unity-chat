using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public class ThreadMessage : Message
    {
        public event Action<ThreadMessage> OnThreadMessageUpdated;

        public string ParentChannelId { get; }

        internal ThreadMessage(Chat chat, string timeToken, string originalMessageText, string channelId,
            string parentChannelId, string userId, PubnubChatMessageType type, Dictionary<string, object> meta,
            List<MessageAction> messageActions) : base(chat, timeToken, originalMessageText, channelId, userId, type,
            meta, messageActions)
        {
            ParentChannelId = parentChannelId;
        }

        protected override SubscribeCallback CreateUpdateListener()
        {
            return chat.ListenerFactory.ProduceListener(
                messageActionCallback: delegate(Pubnub pn, PNMessageActionEventResult e)
                {
                    if (ChatParsers.TryParseMessageUpdate(chat, this, e))
                    {
                        OnThreadMessageUpdated?.Invoke(this);
                    }
                });
        }

        public async Task PinMessageToParentChannel()
        {
            throw new NotImplementedException();
        }

        public async Task UnPinMessageFromParentChannel()
        {
            throw new NotImplementedException();
        }
    }
}