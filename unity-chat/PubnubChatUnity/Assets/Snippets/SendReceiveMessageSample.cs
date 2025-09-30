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

public class SendReceiveMessageSample
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
    
    public static async Task SendMessageExample()
    {
        // snippet.send_message_example
        // reference the channel where you want to send a text message
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        await channel.SendText("Hi everyone!");
        // snippet.end
    }
}
