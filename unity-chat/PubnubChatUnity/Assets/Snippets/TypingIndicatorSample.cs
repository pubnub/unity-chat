// snippet.using
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;
// snippet.end

public class TypingIndicatorSample
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
    
    public static async Task StartTypingExample()
    {
        // snippet.start_typing_example
        // reference the channel where you want to listen to typing signals
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Debug.Log("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;
        // invoke the "startTyping()" method
        await channel.StartTyping();
        // snippet.end
    }
    
    public static async Task StopTypingExample()
    {
        // snippet.stop_typing_example
        // reference the channel where you want to listen to typing signals
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Debug.Log("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;
        // invoke the "StopTyping()" method
        await channel.StopTyping();
        // snippet.end
    }
    
    public static async Task GetTypingEventsExample()
    {
        // snippet.get_typing_events_example
        // reference the channel where you want to listen to typing signals
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // join the channel, start listening for typing
            await channel.Join();
            channel.SetListeningForTyping(true);

            // subscribe to the OnUsersTyping event
            channel.OnUsersTyping += OnUsersTypingHandler;
            
            await Task.Delay(4000);

            // indicate that typing has started
            await channel.StartTyping();
        }
        else
        {
            Debug.Log("Channel not found");
        }
        
        // event handler for typing events
        void OnUsersTypingHandler(List<string> users)
        {
            if (users.Count > 0)
            {
                Debug.Log($"Users typing: {string.Join(", ", users)}");
            }
            else
            {
                Debug.Log("No users are currently typing");
            }
        }
        // snippet.end
    }
}
