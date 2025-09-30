// snippet.using
using System;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class MessageSample
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
    
    public static async Task MessageUpdatedEventExample()
    {
        // snippet.message_updated_event_example
        // Get a channel and a message
        var channelResult = await chat.GetChannel("my_channel");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            var messageResult = await channel.GetMessage("message_timetoken");
            
            if (!messageResult.Error)
            {
                var message = messageResult.Result;
                
                message.OnMessageUpdated += (message) =>
                {
                    Console.WriteLine("Message was edited!");
                };
            }
        }
        // snippet.end
    }
}
