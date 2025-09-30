// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class DeleteChannelSample
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
    
    public static async Task DeleteChannelUsingChannelObjectExample()
    {
        // snippet.delete_channel_using_channel_object_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Debug.Log("Channel to delete doesn't exist.");
            return;
        }
        var channel = channelResult.Result;
        await channel.Delete();
        // snippet.end
    }
    
    public static async Task DeleteChannelUsingChatObjectExample()
    {
        // snippet.delete_channel_using_chat_object_example
        await chat.DeleteChannel("support");
        // snippet.end
    }
}
