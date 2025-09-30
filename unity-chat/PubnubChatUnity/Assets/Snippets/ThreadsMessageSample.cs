// snippet.using
using System;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using Channel = PubnubChatApi.Channel;

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
            Console.WriteLine($"Found channel with name {channel.Name}");

            // fetch the last message from the "support" channel
            // fetch only the last message
            int count = 1;
            var messageHistoryResult = await channel.GetMessageHistory(null, null, count); // Omitting unnecessary time tokens
            if (messageHistoryResult.Error)
            {
                Console.WriteLine("Could not fetch message history.");
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
                    Console.WriteLine($"Could not create thread: {threadChannelResult.Exception.Message}");
                    return;
                }
                var threadChannel = threadChannelResult.Result;

                // (optional) display thread creation information
                Console.WriteLine($"Thread created for message with ID {lastMessage.Id} in channel 'support'.");
                Console.WriteLine($"Thread Channel ID: {threadChannel.Id}");
            }
            else
            {
                Console.WriteLine("No messages found in the 'support' channel.");
            }
        }
        else
        {
            Console.WriteLine("Support channel not found.");
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
            Console.WriteLine($"Found channel with name {channel.Name}");

            // fetch the last message from the "support" channel
            // fetch only the last message
            int count = 1;
            var messageHistoryResult = await channel.GetMessageHistory(null, null, count); // omitting unnecessary timetokens
            if (messageHistoryResult.Error)
            {
                Console.WriteLine("Could not fetch message history.");
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
                    Console.WriteLine($"Could not create thread: {threadChannelResult.Exception.Message}");
                    return;
                }
                var threadChannel = threadChannelResult.Result;

                // (optional) display thread creation information
                Console.WriteLine($"Thread created for message with ID {lastMessage.Id} in channel 'support'.");
                Console.WriteLine($"Thread Channel ID: {threadChannel.Id}");

                // send a reply in the created thread
                string replyMessage = "Good job, guys!";
                await threadChannel.SendText(replyMessage);
                Console.WriteLine($"Sent reply in thread: {replyMessage}");
            }
            else
            {
                Console.WriteLine("No messages found in the 'support' channel.");
            }
        }
        else
        {
            Console.WriteLine("Support channel not found.");
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
                    Console.WriteLine($"Thread channel successfully retrieved: {threadChannel.Name}");
                }
                else
                {
                    Console.WriteLine("No thread channel associated with this message.");
                }
            }
            else
            {
                Console.WriteLine("Message with the given timetoken not found.");
            }
        }
        else
        {
            Console.WriteLine("Channel 'support' not found.");
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
                    Console.WriteLine($"Thread channel successfully retrieved: {threadChannel.Name}");
                }
                else
                {
                    Console.WriteLine("No thread channel associated with this message.");
                }
            }
            else
            {
                Console.WriteLine("Message with the given timetoken not found.");
            }
        }
        else
        {
            Console.WriteLine("Channel 'support' not found.");
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
                    Console.WriteLine("The message starts a thread.");
                }
                else
                {
                    Console.WriteLine("The message does not start a thread.");
                }
            }
            else
            {
                Console.WriteLine("No messages found for the specified time token.");
            }
        }
        else
        {
            Console.WriteLine("Channel 'support' not found.");
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
                    Console.WriteLine($"Thread channel successfully retrieved: {threadChannel.Name}");

                    // subscribe to updates on the thread channel
                    threadChannel.OnChannelUpdate += OnThreadChannelUpdateHandler;
                }
                else
                {
                    Console.WriteLine("No thread channel associated with this message.");
                }
            }
            else
            {
                Console.WriteLine("Message with the given timetoken not found.");
            }
        }
        else
        {
            Console.WriteLine("Channel 'support' not found.");
        }
        
        // handler for thread channel updates
        void OnThreadChannelUpdateHandler(Channel threadChannel)
        {
            Console.WriteLine($"Thread channel updated: {threadChannel.Id}");
        }
        // snippet.end
    }
}
