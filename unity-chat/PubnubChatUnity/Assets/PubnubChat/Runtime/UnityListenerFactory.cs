using System;
using PubnubApi;
using PubnubApi.Unity;
using PubnubChatApi.Utilities;

public class UnityListenerFactory : ChatListenerFactory
{
    public override SubscribeCallback ProduceListener(Action<Pubnub, PNMessageResult<object>> messageCallback = null,
        Action<Pubnub, PNPresenceEventResult> presenceCallback = null,
        Action<Pubnub, PNSignalResult<object>> signalCallback = null,
        Action<Pubnub, PNObjectEventResult> objectEventCallback = null,
        Action<Pubnub, PNMessageActionEventResult> messageActionCallback = null,
        Action<Pubnub, PNFileEventResult> fileCallback = null, Action<Pubnub, PNStatus> statusCallback = null)
    {
        return new SubscribeCallbackListener(messageCallback, presenceCallback, signalCallback, objectEventCallback,
            messageActionCallback, fileCallback, statusCallback);
    }
}