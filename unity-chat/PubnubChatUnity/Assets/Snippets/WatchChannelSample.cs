// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using Channel = PubnubChatApi.Channel;
using UnityEngine;

// snippet.end

public class WatchChannelSample
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
    
    public static async Task WatchChannelExample()
    {
        // snippet.watch_channel_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Debug.Log("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        channel.OnMessageReceived += OnMessageReceivedHandler; // or use lambda

        void OnMessageReceivedHandler(Message message)
        {
            Debug.Log($"Message received: {message.MessageText}");
        }

        channel.Connect();
        // snippet.end
    }
    
    public static void UnwatchChannelExample(Channel channel)
    {
        // snippet.unwatch_channel_example
        void OnMessageReceivedHandler(Message message)
        {
            Debug.Log($"Message received: {message.MessageText}");
            channel.Disconnect();
            Debug.Log("Disconnected from the channel.");
        }
        // snippet.end
    }
}
