// snippet.using
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class RestoreMessageSample
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
    
    public static async Task RestoreMessageExample()
    {
        // snippet.restore_message_example
        // reference the "channel" object
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Debug.Log("Channel 'support' not found.");
            return;
        }
        var channel = channelResult.Result;
        Debug.Log($"Found channel with name {channel.Name}");

        // invoke the method on the "channel" object to get message history
        var messagesResult = await channel.GetMessageHistory(
            "16200000000000000", 
            "16200000000000001", 
            1
        );
        if (messagesResult.Error || !messagesResult.Result.Any())
        {
            Debug.Log("Message not found.");
            return;
        }

        // Find the specific message with the given timetoken
        var message = messagesResult.Result[0];

        // Restore the message
        await message.Restore();
        // snippet.end
    }
}
