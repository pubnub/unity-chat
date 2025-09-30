// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class ForwardMessageSample
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
    
    public static async Task ForwardMessageExample()
    {
        // snippet.forward_message_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Debug.Log("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        // reference a message on the "support" channel
        var messageResult = await channel.GetMessage("16686902600029072");
        if (messageResult.Error) 
        {
            Debug.Log("Couldn't find message!");
            return;
        }
        var message = messageResult.Result;

        // use the "forward()" method to send the message to the "incident-management" channel
        await message.Forward("incident-management");
        // snippet.end
    }
    
    public static async Task ForwardMessageUsingChannelExample()
    {
        // snippet.forward_message_using_channel_example
        var originalChannelResult = await chat.GetChannel("support");
        if (originalChannelResult.Error)
        {
            Debug.Log("Couldn't find original channel!");
            return;
        }
        var originalChannel = originalChannelResult.Result;

        // reference a message on the "support" channel
        var messageResult = await originalChannel.GetMessage("16686902600029072");
        if (messageResult.Error) 
        {
            Debug.Log("Couldn't find message!");
            return;
        }
        var message = messageResult.Result;

        // reference the "incident-management" channel to which you want to forward the message
        var channelResult = await chat.GetChannel("incident-management");
        if (channelResult.Error)
        {
            Debug.Log("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        // use the "ForwardMessage()" method to send the message to the "incident-management" channel
        await channel.ForwardMessage(message);
        // snippet.end
    }
}
