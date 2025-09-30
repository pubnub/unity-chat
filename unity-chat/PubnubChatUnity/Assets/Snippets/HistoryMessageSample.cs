// snippet.using
using System;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class HistoryMessageSample
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
    
    public static async Task GetMessageHistoryExample()
    {
        // snippet.get_message_history_example
        // reference the "channel" object
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        // invoke the method on the "channel" object
        var messagesResult = await channel.GetMessageHistory("15343325214676133", null, 10);
        var messages = messagesResult.Result;
        // snippet.end
    }
}
