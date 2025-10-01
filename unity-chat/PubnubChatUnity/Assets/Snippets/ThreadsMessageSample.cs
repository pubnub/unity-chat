// snippet.using
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using Channel = PubnubChatApi.Channel;
using UnityEngine;

// snippet.end

public class ThreadsMessageSample
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
    
    public static async Task CreateThreadExample()
    {
        // snippet.create_thread_example
        // retrieve the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // fetch the last message from the "support" channel
            // fetch only the last message
            int count = 1;
            var messageHistoryResult = await channel.GetMessageHistory(null, null, count); // Omitting unnecessary time tokens
            if (messageHistoryResult.Error)
            {
                Debug.Log("Could not fetch message history.");
                return;
            }
            var lastMessage = messageHistoryResult.Result.FirstOrDefault();

            // check if there are any messages
            if (lastMessage != null)
            {
                // call the CreateThread method on the last message
                var threadChannelResult = lastMessage.CreateThread();
                if (threadChannelResult.Error)
                {
                    Debug.Log($"Could not create thread: {threadChannelResult.Exception.Message}");
                    return;
                }
                var threadChannel = threadChannelResult.Result;

                // (optional) display thread creation information
                Debug.Log($"Thread created for message with ID {lastMessage.Id} in channel 'support'.");
                Debug.Log($"Thread Channel ID: {threadChannel.Id}");
            }
            else
            {
                Debug.Log("No messages found in the 'support' channel.");
            }
        }
        else
        {
            Debug.Log("Support channel not found.");
        }
        // snippet.end
    }
    
    public static async Task SendThreadMessageExample()
    {
        // snippet.send_thread_message_example
        // retrieve the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // fetch the last message from the "support" channel
            // fetch only the last message
            int count = 1;
            var messageHistoryResult = await channel.GetMessageHistory(null, null, count); // omitting unnecessary timetokens
            if (messageHistoryResult.Error)
            {
                Debug.Log("Could not fetch message history.");
                return;
            }
            var lastMessage = messageHistoryResult.Result.FirstOrDefault();

            // check if there are any messages
            if (lastMessage != null)
            {
                // call the CreateThread method on the last message
                var threadChannelResult = lastMessage.CreateThread();
                if (threadChannelResult.Error)
                {
                    Debug.Log($"Could not create thread: {threadChannelResult.Exception.Message}");
                    return;
                }
                var threadChannel = threadChannelResult.Result;

                // (optional) display thread creation information
                Debug.Log($"Thread created for message with ID {lastMessage.Id} in channel 'support'.");
                Debug.Log($"Thread Channel ID: {threadChannel.Id}");

                // send a reply in the created thread
                string replyMessage = "Good job, guys!";
                await threadChannel.SendText(replyMessage);
                Debug.Log($"Sent reply in thread: {replyMessage}");
            }
            else
            {
                Debug.Log("No messages found in the 'support' channel.");
            }
        }
        else
        {
            Debug.Log("Support channel not found.");
        }
        // snippet.end
    }
    
    public static async Task GetThreadUsingGetThreadExample()
    {
        // snippet.get_thread_using_get_thread_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            // get the message with the specific timetoken
            var messageResult = await channel.GetMessage("16200000000000001");
            if (!messageResult.Error)
            {
                var message = messageResult.Result;
                // get the thread channel created from the message
                var threadChannelResult = await message.GetThread();
                if (!threadChannelResult.Error)
                {
                    var threadChannel = threadChannelResult.Result;
                    Debug.Log($"Thread channel successfully retrieved: {threadChannel.Name}");
                }
                else
                {
                    Debug.Log("No thread channel associated with this message.");
                }
            }
            else
            {
                Debug.Log("Message with the given timetoken not found.");
            }
        }
        else
        {
            Debug.Log("Channel 'support' not found.");
        }
        // snippet.end
    }
    
    public static async Task GetThreadUsingGetThreadChannelExample()
    {
        // snippet.get_thread_using_get_thread_channel_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            // get the message with the specific timetoken
            var messageResult = await channel.GetMessage("16200000000000001");
            if (!messageResult.Error)
            {
                var message = messageResult.Result;
                // get the thread channel created from the message
                var threadChannelResult = await chat.GetThreadChannel(message);
                if (!threadChannelResult.Error)
                {
                    var threadChannel = threadChannelResult.Result;
                    Debug.Log($"Thread channel successfully retrieved: {threadChannel.Name}");
                }
                else
                {
                    Debug.Log("No thread channel associated with this message.");
                }
            }
            else
            {
                Debug.Log("Message with the given timetoken not found.");
            }
        }
        else
        {
            Debug.Log("Channel 'support' not found.");
        }
        // snippet.end
    }
    
    public static async Task CheckIfMessageHasThreadExample()
    {
        // snippet.check_if_message_has_thread_example
        // reference the channel object
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            // get the message history that includes the message with the specific time token
            var messagesResult = await channel.GetMessageHistory("16200000000000000", null, 1);

            // assuming we get exactly one message in response
            if (!messagesResult.Error && messagesResult.Result.Count > 0)
            {
                var message = messagesResult.Result[0];

                // check if the message starts a thread
                if (message.HasThread())
                {
                    Debug.Log("The message starts a thread.");
                }
                else
                {
                    Debug.Log("The message does not start a thread.");
                }
            }
            else
            {
                Debug.Log("No messages found for the specified time token.");
            }
        }
        else
        {
            Debug.Log("Channel 'support' not found.");
        }
        // snippet.end
    }
    
    public static async Task GetThreadChannelUpdatesExample()
    {
        // snippet.get_thread_channel_updates_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            // get the message with the specific timetoken
            var messageResult = await channel.GetMessage("16200000000000001");
            if (!messageResult.Error)
            {
                var message = messageResult.Result;
                // get the thread channel created from the message
                var threadChannelResult = await message.GetThread();
                if (!threadChannelResult.Error)
                {
                    var threadChannel = threadChannelResult.Result;
                    Debug.Log($"Thread channel successfully retrieved: {threadChannel.Name}");

                    // subscribe to updates on the thread channel
                    threadChannel.OnChannelUpdate += OnThreadChannelUpdateHandler;
                }
                else
                {
                    Debug.Log("No thread channel associated with this message.");
                }
            }
            else
            {
                Debug.Log("Message with the given timetoken not found.");
            }
        }
        else
        {
            Debug.Log("Channel 'support' not found.");
        }
        
        // handler for thread channel updates
        void OnThreadChannelUpdateHandler(Channel threadChannel)
        {
            Debug.Log($"Thread channel updated: {threadChannel.Id}");
        }
        // snippet.end
    }
    
    public static async Task GetHistoricalThreadMessagesExample()
    {
        // snippet.get_historical_thread_messages_example
        // reference the "channel" object
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");
            
            // get the last message in the channel
            var messageHistoryResult = await channel.GetMessageHistory(null, null, 1);
            var lastMessage = messageHistoryResult.Result.FirstOrDefault();
            if (lastMessage != null)
            {
                Debug.Log($"Found last message with timetoken {lastMessage.TimeToken}");
                
                // check if the last message has a thread and fetch its thread channel
                var threadChannelResult = await lastMessage.GetThread();
                if (!threadChannelResult.Error)
                {
                    var threadChannel = threadChannelResult.Result;
                    Debug.Log($"Thread channel successfully retrieved: {threadChannel.Name}");
                    
                    // fetch 10 historical thread messages older than timetoken 15343325214676133
                    var threadMessagesResult = await threadChannel.GetMessageHistory("15343325214676133", null, 10);
                    if (!threadMessagesResult.Error)
                    {
                        Debug.Log($"Retrieved {threadMessagesResult.Result.Count} historical thread messages.");
                        foreach (var threadMessage in threadMessagesResult.Result)
                        {
                            Debug.Log($"Thread message: {threadMessage.MessageText}");
                        }
                    }
                    else
                    {
                        Debug.Log("Could not retrieve historical thread messages.");
                    }
                }
                else
                {
                    Debug.Log("No thread channel associated with this message.");
                }
            }
            else
            {
                Debug.Log("No messages found in the 'support' channel.");
            }
        }
        else
        {
            Debug.Log("Support channel not found.");
        }
        // snippet.end
    }
    
    public static async Task RemoveThreadExample()
    {
        // snippet.remove_thread_example
        // reference the "channel" object
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");
            
            // get the last message in the channel
            var messageHistoryResult = await channel.GetMessageHistory(null, null, 1);
            var lastMessage = messageHistoryResult.Result.FirstOrDefault();
            if (lastMessage != null)
            {
                Debug.Log($"Found last message with timetoken {lastMessage.TimeToken}");
                
                // remove the thread for the last message
                await lastMessage.RemoveThread();
                Debug.Log("Thread removed successfully.");
            }
            else
            {
                Debug.Log("No messages found in the 'support' channel.");
            }
        }
        else
        {
            Debug.Log("Support channel not found.");
        }
        // snippet.end
    }
    
    public static async Task PinMessageToThreadChannelExample()
    {
        // snippet.pin_message_to_thread_channel_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // get the last message on the channel, which is the root message for the thread
            var channelMessageHistoryResult = await channel.GetMessageHistory(null, null, 1);
            var lastChannelMessage = channelMessageHistoryResult.Result.FirstOrDefault();
            if (lastChannelMessage != null)
            {
                Debug.Log($"Found last channel message with timetoken {lastChannelMessage.TimeToken}");

                // get the thread channel created from the message
                var threadChannelResult = await lastChannelMessage.GetThread();
                if (!threadChannelResult.Error)
                {
                    var threadChannel = threadChannelResult.Result;
                    Debug.Log($"Thread channel successfully retrieved: {threadChannel.Name}");

                    // get the last message from the thread channel
                    var threadMessageHistoryResult = await threadChannel.GetMessageHistory(null, null, 1);
                    var lastThreadMessage = threadMessageHistoryResult.Result.FirstOrDefault();
                    if (lastThreadMessage != null)
                    {
                        Debug.Log($"Found last thread message with timetoken {lastThreadMessage.TimeToken}");

                        // pin the last thread message to the thread channel
                        await threadChannel.PinMessage(lastThreadMessage);
                        Debug.Log("Message pinned to thread channel successfully.");
                    }
                    else
                    {
                        Debug.Log("No messages found in the thread channel.");
                    }
                }
                else
                {
                    Debug.Log("No thread channel associated with this message.");
                }
            }
            else
            {
                Debug.Log("No messages found in the 'support' channel.");
            }
        }
        else
        {
            Debug.Log("Support channel not found.");
        }
        // snippet.end
    }
    
    public static async Task PinMessageToParentChannelExample()
    {
        // snippet.pin_message_to_parent_channel_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // get the last message on the channel, which is the root message for the thread
            var channelMessageHistoryResult = await channel.GetMessageHistory(null, null, 1);
            var lastChannelMessage = channelMessageHistoryResult.Result.FirstOrDefault();
            if (lastChannelMessage != null)
            {
                Debug.Log($"Found last channel message with timetoken {lastChannelMessage.TimeToken}");

                // get the thread channel created from the message
                var threadChannelResult = await lastChannelMessage.GetThread();
                if (!threadChannelResult.Error)
                {
                    var threadChannel = threadChannelResult.Result;
                    Debug.Log($"Thread channel successfully retrieved: {threadChannel.Name}");

                    // get the last message from the thread channel
                    var threadMessageHistoryResult = await threadChannel.GetMessageHistory(null, null, 1);
                    var lastThreadMessage = threadMessageHistoryResult.Result.FirstOrDefault();
                    if (lastThreadMessage != null)
                    {
                        Debug.Log($"Found last thread message with timetoken {lastThreadMessage.TimeToken}");

                        // pin the last thread message to the parent channel
                        await channel.PinMessage(lastThreadMessage);
                        Debug.Log("Thread message pinned to parent channel successfully.");
                    }
                    else
                    {
                        Debug.Log("No messages found in the thread channel.");
                    }
                }
                else
                {
                    Debug.Log("No thread channel associated with this message.");
                }
            }
            else
            {
                Debug.Log("No messages found in the 'support' channel.");
            }
        }
        else
        {
            Debug.Log("Support channel not found.");
        }
        // snippet.end
    }
    
    public static async Task UnpinMessageFromThreadChannelExample()
    {
        // snippet.unpin_message_from_thread_channel_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // get the last message on the channel, which is the root message for the thread
            var channelMessageHistoryResult = await channel.GetMessageHistory(null, null, 1);
            var lastChannelMessage = channelMessageHistoryResult.Result.FirstOrDefault();
            if (lastChannelMessage != null)
            {
                Debug.Log($"Found last channel message with timetoken {lastChannelMessage.TimeToken}");

                // get the thread channel created from the message
                var threadChannelResult = await lastChannelMessage.GetThread();
                if (!threadChannelResult.Error)
                {
                    var threadChannel = threadChannelResult.Result;
                    Debug.Log($"Thread channel successfully retrieved: {threadChannel.Name}");

                    // unpin the message from the thread channel
                    await threadChannel.UnpinMessage();
                    Debug.Log("Message unpinned from thread channel successfully.");
                }
                else
                {
                    Debug.Log("No thread channel associated with this message.");
                }
            }
            else
            {
                Debug.Log("No messages found in the 'support' channel.");
            }
        }
        else
        {
            Debug.Log("Support channel not found.");
        }
        // snippet.end
    }
    
    public static async Task UnpinMessageFromParentChannelExample()
    {
        // snippet.unpin_message_from_parent_channel_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // unpin the message from the parent channel
            await channel.UnpinMessage();
            Debug.Log("Message unpinned from parent channel successfully.");
        }
        else
        {
            Debug.Log("Support channel not found.");
        }
        // snippet.end
    }
}
