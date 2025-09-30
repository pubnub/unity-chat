// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class ChannelSample
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
    
    public static async Task EventSubscriptionExample()
    {
        // snippet.event_subscription_example
        // Get or create a channel
        var channelResult = await chat.GetChannel("my_channel");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            
            channel.OnMessageReceived += (message) =>
            {
                Debug.Log("New message received!");
            };
        }
        // snippet.end
    }
}
