// snippet.using
using System;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class DetailsMessageSample
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
    
    public static async Task GetMessageDetailsExample()
    {
        // snippet.get_message_details_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        // get the message
        var messageResult = await channel.GetMessage("16200000000000001");
        if (!messageResult.Error)
        {
            var message = messageResult.Result;
            Console.WriteLine($"Message: {message.MessageText}");
        }
        // snippet.end
    }
    
    public static async Task GetMessageContentExample()
    {
        // snippet.get_message_content_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        // get the message
        var messageResult = await channel.GetMessage("16200000000000001"); 
        if (!messageResult.Error) 
        {
            var message = messageResult.Result;
            Console.WriteLine($"Message: {message.MessageText}");
        }
        // snippet.end
    }
    
    public static async Task CheckDeletionStatusExample()
    {
        // snippet.check_deletion_status_example
        // get the message
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        // get the message
        var messageResult = await channel.GetMessage("16200000000000000"); 
        if (!messageResult.Error) 
        {
           var message = messageResult.Result;
           Console.WriteLine($"Is deleted?: {message.IsDeleted}");
        }
        // snippet.end
    }
}
