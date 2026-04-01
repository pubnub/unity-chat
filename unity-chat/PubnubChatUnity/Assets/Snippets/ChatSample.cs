// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class ChatSample
{
    private static Chat chat;

    static async Task Init()
    {
        // snippet.init
        // Configuration
        PubnubChatConfig chatConfig = new PubnubChatConfig();
        
        PNConfiguration pnConfiguration = new PNConfiguration(new UserId("myUniqueUserId"))
        {
            SubscribeKey = "demo",
            PublishKey = "demo",
            Secure = true
        };

        // Initialize Unity Chat
        var chatResult = await UnityChat.CreateInstance(chatConfig, pnConfiguration);
        if (!chatResult.Error)
        {
            chat = chatResult.Result;
        }
        // snippet.end
    }
    
    public static void AnyEventSubscriptionExample()
    {
        // snippet.any_event_subscription_example
        chat.OnAnyEvent += (chatEvent) =>
        {
            Debug.Log($"New event of type {chatEvent.Type} received!");
        };
        // snippet.end
    }

    public static void StatusListenerExample()
    {
        // snippet.status_listener_example
        chat.StreamSubscriptionStatus(true);
        chat.OnSubscriptionStatusChanged += status =>
        {
            Debug.Log($"Staus categoru: {status.Category}, affected channels: {status.AffectedChannels}");
        };
        // snippet.end
    }
    
    public static async void ReconnectExample()
    {
        // snippet.disconnect_and_reconnect_example
        //Will reconnect all existing subscriptions
        await chat.ReconnectSubscriptions();

        //Will disconnect all existing subscriptions
        await chat.DisconnectSubscriptions();
        // snippet.end
    }
}
