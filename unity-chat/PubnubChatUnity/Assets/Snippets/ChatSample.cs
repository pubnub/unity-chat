// snippet.using
using System;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
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
            Console.WriteLine($"New event of type {chatEvent.Type} received!");
        };
        // snippet.end
    }
}
