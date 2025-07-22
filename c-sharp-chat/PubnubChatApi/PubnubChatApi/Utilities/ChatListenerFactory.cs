using System;
using PubnubApi;

namespace PubnubChatApi.Utilities
{
    public abstract class ChatListenerFactory
    {
        public abstract SubscribeCallback ProduceListener(Action<Pubnub, PNMessageResult<object>>? messageCallback = null,
            Action<Pubnub, PNPresenceEventResult>? presenceCallback = null,
            Action<Pubnub, PNSignalResult<object>>? signalCallback = null,
            Action<Pubnub, PNObjectEventResult>? objectEventCallback = null,
            Action<Pubnub, PNMessageActionEventResult>? messageActionCallback = null,
            Action<Pubnub, PNFileEventResult>? fileCallback = null,
            Action<Pubnub, PNStatus>? statusCallback = null);
    }
}