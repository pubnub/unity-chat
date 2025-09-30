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
            channel.SetListeningForReadReceiptsEvents(true);

            // subscribe to the OnReadReceiptEvent event
            channel.OnReadReceiptEvent += OnReadHandler;
        }
        else
        {
            Debug.Log("Channel not found");
        }
        
        // the event handler
        void OnReadHandler(Dictionary<string, List<string>> readEvent)
        {
            // print the message details to the console
            foreach (var kvp in readEvent)
            {
                var channel = kvp.Key;
                foreach (var user in kvp.Value)
                {
                    Debug.Log(
                        $"Received a read receipt event on channel {channel}" +
                        $" from user {user}");   
                }
            }
            // you can add additional logic here, such as confirming receipt to the user or processing the message further
        }
        // snippet.end
    }
}
