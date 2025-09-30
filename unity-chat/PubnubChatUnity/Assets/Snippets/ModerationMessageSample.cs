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

public class ModerationMessageSample
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
    
    public static async Task ReportMessageExample()
    {
        // snippet.report_message_example
        // get the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Support channel not found.");
            return;
        }
        var channel = channelResult.Result;

        Console.WriteLine($"Found channel with name {channel.Name}");

        // retrieve the message history with the desired count
        var messageHistoryResult = await channel.GetMessageHistory(null, null, 1);
        if (messageHistoryResult.Error)
        {
            Console.WriteLine("Could not retrieve message history.");
            return;
        }

        // get the last message from the returned list
        var lastMessage = messageHistoryResult.Result.FirstOrDefault();

        // report the last message if it exists
        if (lastMessage != null)
        {
            await lastMessage.Report("This is insulting!");
            Console.WriteLine("Reported the last message in the support channel.");
        }
        else
        {
            Console.WriteLine("No messages found in the channel history.");
        }
        // snippet.end
    }
    
    public static async Task ListenToReportEventsExample()
    {
        // snippet.listen_to_report_events_example
        var channelResult = await chat.GetChannel("support");
        if(channelResult.Error){
            Console.WriteLine("Couldn't find the support channel!");
            return;
        }
        var channel = channelResult.Result;
        channel.SetListeningForReportEvents(true);
        channel.OnReportEvent += reportEvent => 
        {
            Console.WriteLine("Message reported on the support channel!");
        };
        // snippet.end
    }
}
