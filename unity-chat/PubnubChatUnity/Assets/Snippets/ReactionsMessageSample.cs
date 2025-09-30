// snippet.using
using System;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class ReactionsMessageSample
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
    
    public static async Task AddReactionExample()
    {
        // snippet.add_reaction_example
        // reference the "support" channel and ensure it's found
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Support channel not found.");
            return;
        }
        var channel = channelResult.Result;
        Console.WriteLine($"Found channel with name {channel.Name}");

        // get the message history with the desired count
        var messageHistoryResult = await channel.GetMessageHistory(null, null, 1);
        if (messageHistoryResult.Error)
        {
            Console.WriteLine("Could not retrieve message history.");
            return;
        }

        // get the last message from the returned list
        var lastMessage = messageHistoryResult.Result.FirstOrDefault();

        if (lastMessage != null)
        {
            // add the "thumb up" emoji to the last message
            await lastMessage.ToggleReaction("\\u{1F44D}");
            Console.WriteLine("Added 'thumb up' reaction to the last message.");
        }
        else
        {
            Console.WriteLine("No messages found in the channel history.");
        }
        // snippet.end
    }
    
    public static async Task GetReactionsExample()
    {
        // snippet.get_reactions_example
        // reference the "support" channel and ensure it's found
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Support channel not found.");
            return;
        }
        var channel = channelResult.Result;
        Console.WriteLine($"Found channel with name {channel.Name}");

        // get the message history with the desired count
        var messageHistoryResult = await channel.GetMessageHistory(null, null, 1);
        if (messageHistoryResult.Error)
        {
            Console.WriteLine("Could not retrieve message history.");
            return;
        }

        // get the last message from the returned list
        var lastMessage = messageHistoryResult.Result.FirstOrDefault();

        if (lastMessage != null)
        {
            // output all reactions added to the last message
            var reactions = lastMessage.Reactions;
            foreach (var reaction in reactions)
            {
                Console.WriteLine($"Reaction: {reaction.Value}");
            }
        }
        else
        {
            Console.WriteLine("No messages found in the channel history.");
        }
        // snippet.end
    }
    
    public static async Task CheckUserReactionExample()
    {
        // snippet.check_user_reaction_example
        // reference the "support" channel and ensure it's found
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Support channel not found.");
            return;
        }
        var channel = channelResult.Result;
        Console.WriteLine($"Found channel with name {channel.Name}");

        // get the message history with the desired count
        var messageHistoryResult = await channel.GetMessageHistory(null, null, 1);
        if (messageHistoryResult.Error)
        {
            Console.WriteLine("Could not retrieve message history.");
            return;
        }

        // get the last message from the returned list
        var lastMessage = messageHistoryResult.Result.FirstOrDefault();

        if (lastMessage != null)
        {
            // Check if the current user added the "thumb up" emoji to the last message
            if (lastMessage.HasUserReaction("\\u{1F44D}"))
            {
                Console.WriteLine("The current user has added a 'thumb up' reaction to the last message.");
            }
            else
            {
                Console.WriteLine("The current user has not added a 'thumb up' reaction to the last message.");
            }
        }
        else
        {
            Console.WriteLine("No messages found in the channel history.");
        }
        // snippet.end
    }
}
