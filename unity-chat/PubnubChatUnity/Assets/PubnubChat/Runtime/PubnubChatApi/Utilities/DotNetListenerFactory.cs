using System;
using PubnubApi;

namespace PubnubChatApi.Utilities
{
    public class DotNetListenerFactory : ChatListenerFactory
    {
        public override SubscribeCallback ProduceListener(
            Action<Pubnub, PNMessageResult<object>>? messageCallback = null,
            Action<Pubnub, PNPresenceEventResult>? presenceCallback = null,
            Action<Pubnub, PNSignalResult<object>>? signalCallback = null,
            Action<Pubnub, PNObjectEventResult>? objectEventCallback = null,
            Action<Pubnub, PNMessageActionEventResult>? messageActionCallback = null,
            Action<Pubnub, PNFileEventResult>? fileCallback = null, Action<Pubnub, PNStatus>? statusCallback = null)
        {
            return new SubscribeCallbackExt(messageCallback, presenceCallback, signalCallback, objectEventCallback,
                messageActionCallback, fileCallback, statusCallback);
        }
    }
}