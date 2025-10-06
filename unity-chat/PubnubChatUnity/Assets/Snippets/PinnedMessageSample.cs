// snippet.using
using System;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class PinnedMessageSample
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
    
    public static async Task PinMessageUsingPinExample()
    {
        // snippet.pin_message_using_pin_example
        // get the "incident-management" channel
        var channelResult = await chat.GetChannel("incident-management");
        if (channelResult.Error)
        {
            Debug.Log("Incident-management channel not found.");
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

        // pin the last message if it exists
        if (lastMessage != null)
        {
            await lastMessage.Pin();
            Debug.Log("Pinned the last message in the incident-management channel.");
        }
        else
        {
            Debug.Log("No messages found in the channel history.");
        }
        // snippet.end
    }
    
    public static async Task PinMessageUsingChannelExample()
    {
        // snippet.pin_message_using_channel_example
        // get the "incident-management" channel
        var channelResult = await chat.GetChannel("incident-management");
        if (channelResult.Error)
        {
            Debug.Log("Incident-management channel not found.");
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

        // pin the last message using the PinMessage method if it exists
        if (lastMessage != null)
        {
            await channel.PinMessage(lastMessage);
            Debug.Log("Pinned the last message in the incident-management channel.");
        }
        else
        {
            Debug.Log("No messages found in the channel history.");
        }
        // snippet.end
    }
    
    public static async Task GetPinnedMessageExample()
    {
        // snippet.get_pinned_message_example
        var channelResult = await chat.GetChannel("incident-management");
        if (channelResult.Error)
        {
            Debug.Log("Channel 'incident-management' not found.");
            return;
        }
        var channel = channelResult.Result;
        Debug.Log($"Found channel with name {channel.Name}");

        // Try to get the pinned message from the channel
        var pinnedMessageResult = await channel.GetPinnedMessage();
        if (!pinnedMessageResult.Error)
        {
            var pinnedMessage = pinnedMessageResult.Result;
            Debug.Log("Pinned message found: " + pinnedMessage.MessageText);
        }
        else
        {
            Debug.Log("No pinned message found.");
        }
        // snippet.end
    }
    
    public static async Task UnpinMessageExample()
    {
        // snippet.unpin_message_example
        // attempt to get the channel named "incident-management"
        var channelResult = await chat.GetChannel("incident-management");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // attempt to unpin a message
            try
            {
                await channel.UnpinMessage();
                Debug.Log("Message has been unpinned successfully.");
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to unpin the message: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("Channel 'incident-management' not found.");
        }
        // snippet.end
    }
}
