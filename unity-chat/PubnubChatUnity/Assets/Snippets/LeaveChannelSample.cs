// snippet.using
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubApi.Unity;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using PubnubChat.Runtime;
using UnityEngine;
// snippet.end

public class LeaveChannelSample
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
    
    public static async Task LeaveChannelExample()
    {
        // snippet.leave_channel_example
        // reference the "channel" object
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        channel.OnMessageReceived += OnMessageReceivedHandler; // or use lambda
        void OnMessageReceivedHandler(Message message)
        {
            Console.WriteLine($"Message received: {message.MessageText}");
        }
        // join the channel and add metadata to the newly created membership
        await channel.Join();
        // and leave
        await channel.Leave();
        // snippet.end
    }
}
