// snippet.using
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
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
            Debug.Log("Support channel not found.");
            return;
        }
        var channel = channelResult.Result;

        Debug.Log($"Found channel with name {channel.Name}");

        // retrieve the message history with the desired count
        var messageHistoryResult = await channel.GetMessageHistory(null, null, 1);
        if (messageHistoryResult.Error)
        {
            Debug.Log("Could not retrieve message history.");
            return;
        }

        // get the last message from the returned list
        var lastMessage = messageHistoryResult.Result.FirstOrDefault();

        // report the last message if it exists
        if (lastMessage != null)
        {
            await lastMessage.Report("This is insulting!");
            Debug.Log("Reported the last message in the support channel.");
        }
        else
        {
            Debug.Log("No messages found in the channel history.");
        }
        // snippet.end
    }
    
    public static async Task ListenToReportEventsExample()
    {
        // snippet.listen_to_report_events_example
        var channelResult = await chat.GetChannel("support");
        if(channelResult.Error){
            Debug.Log("Couldn't find the support channel!");
            return;
        }
        var channel = channelResult.Result;
        channel.StreamReportEvents(true);
        channel.OnReportEvent += reportEvent => 
        {
            Debug.Log("Message reported on the support channel!");
        };
        // snippet.end
    }
}
