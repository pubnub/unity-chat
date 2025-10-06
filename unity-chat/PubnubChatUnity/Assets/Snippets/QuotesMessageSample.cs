// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class QuotesMessageSample
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
    
    public static async Task QuoteMessageExample()
    {
        // snippet.quote_message_example
        var quotedMessageResult = await chat.GetMessage("support", "16200000000000001");
        if(quotedMessageResult.Error)
        {
            return;
        }
        var quotedMessage = quotedMessageResult.Result;
        
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error) return;
        var testChannel = channelResult.Result;
        
        await testChannel.SendText("message with a quote", new SendTextParams()
            {
                QuotedMessage = quotedMessage
            });
        // snippet.end
    }
    
    public static async Task GetQuotedMessageExample()
    {
        // snippet.get_quoted_message_example
        var channelId = "support";
        var messageTimeToken = "16200000000000001";

        // retrieve the channel details
        var channelResult = await chat.GetChannel(channelId);
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // retrieve the specific message by its timetoken
            var messageResult = await channel.GetMessage(messageTimeToken);
            if (!messageResult.Error) 
            {
                var message = messageResult.Result;
                // try to get the quoted message
                var quotedMessageResult = await message.GetQuotedMessage();
                if (!quotedMessageResult.Error)
                {
                    var quotedMessage = quotedMessageResult.Result;
                    Debug.Log($"Quoted message: {quotedMessage.MessageText}");
                }
                else
                {
                    Debug.Log("No quoted message found.");
                }
            }
            else
            {
                Debug.Log("Message not found.");
            }
        }
        else
        {
            Debug.Log("Channel not found.");
        }
        // snippet.end
    }
}
