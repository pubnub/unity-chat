// snippet.using
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using PubnubApi;
using PubnubApi.Unity;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using PubnubChat.Runtime;
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
            Console.WriteLine("Channel 'support' not found.");
            return;
        }
        var channel = channelResult.Result;
        Console.WriteLine($"Found channel with name {channel.Name}");

        // invoke the method on the "channel" object to get message history
        var messagesResult = await channel.GetMessageHistory(
            "16200000000000000", 
            "16200000000000001", 
            1
        );
        if (messagesResult.Error || !messagesResult.Result.Any())
        {
            Console.WriteLine("Message not found.");
            return;
        }

        // Find the specific message with the given timetoken
        var message = messagesResult.Result[0];

        // Restore the message
        await message.Restore();
        // snippet.end
    }
}
