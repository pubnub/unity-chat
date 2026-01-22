// snippet.using
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class ReadReceiptsMessageSample
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
    
    public static async Task ReadReceiptsExample()
    {
        // snippet.read_receipts_example
        // reference the channel where you want to listen to message signals
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // join the channel and start listening to read receipt events
            await channel.Join();
            channel.StreamReadReceipts(true);

            // subscribe to the OnReadReceiptEvent event
            channel.OnReadReceiptEvent += OnReadHandler;
        }
        else
        {
            Debug.Log("Channel not found");
        }
        
        // the event handler
        void OnReadHandler((string MessageTimetoken, string UserId) readEvent)
        {
            // print the message details to the console
            Debug.Log(
                $"Received a read receipt event for timetoken {readEvent.MessageTimetoken}" +
                $" from user {readEvent.UserId}");  
            // you can add additional logic here, such as confirming receipt to the user or processing the message further
        }
        // snippet.end
    }

    public static async Task ReadReceiptsConfigExample()
    {
        // snippet.read_receipts_config_example
        // set read receipt emission rules per channel type when creating Chat object
        var chatConfig = new PubnubChatConfig()
        {
            EmitReadReceiptEvents =
            {
                { "public", false },
                { "group", true },
                { "direct", true },
                { "some_custom_type", true },
            }
        };
        var pubnubConfig = new PNConfiguration(new UserId("some_user"))
        {
            PublishKey = "your_publish_key",
            SubscribeKey = "your_subscribe_key",
        };
        var createChat = await UnityChat.CreateInstance(chatConfig, pubnubConfig);
        if (createChat.Error)
        {
            Debug.LogError($"Error when trying to create Chat instance: {createChat.Exception.Message}");
            return;
        }
        var chat = createChat.Result;
        // snippet.end
    }
    
    public static async Task ReadReceiptsInstanceExample()
    {
        // snippet.read_receipts_instance_example
        var getChannel = await chat.GetChannel("some_channel");
        if (getChannel.Error)
        {
            Debug.LogError($"Error when trying to get channel: {getChannel.Exception.Message}");
            return;
        }
        var channel = getChannel.Result;

        // this means that even if PubnubChatConfig has emitting read receipt events set to false
        // for this type of channel, this instance will emit them
        await channel.Update(new ChatChannelData() { EmitReadReceiptEvents = true });
        // snippet.end
    }
}
