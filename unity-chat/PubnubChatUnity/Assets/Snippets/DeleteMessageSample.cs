// snippet.using
using System;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class DeleteMessageSample
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
    
    public static async Task PermanentDeleteMessageExample()
    {
        // snippet.permanent_delete_message_example
        // reference the "channel" object
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        // invoke the method on the "channel" object
        var messagesResult = await channel.GetMessageHistory("16200000000000000", "16200000000000001", 1);
        if (messagesResult.Error || !messagesResult.Result.Any())
        {
            Console.WriteLine("Couldn't find message!");
            return;
        }
        var message = messagesResult.Result[0];

        // permanently remove the message
        await message.Delete(false);
        // snippet.end
    }
    
    public static async Task SoftDeleteMessageExample()
    {
        // snippet.soft_delete_message_example
        // reference the "channel" object
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        // invoke the method on the "channel" object
        var messagesResult = await channel.GetMessageHistory("16200000000000000", "16200000000000001", 1);
        if (messagesResult.Error || !messagesResult.Result.Any())
        {
            Console.WriteLine("Couldn't find message!");
            return;
        }
        var message = messagesResult.Result[0];

        // soft delete the message
        await message.Delete(soft: true);
        // snippet.end
    }
}
