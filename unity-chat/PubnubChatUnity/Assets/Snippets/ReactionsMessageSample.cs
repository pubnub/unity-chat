// snippet.using
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

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
            Debug.Log("Support channel not found.");
            return;
        }
        var channel = channelResult.Result;
        Debug.Log($"Found channel with name {channel.Name}");

        // get the message history with the desired count
        var messageHistoryResult = await channel.GetMessageHistory(null, null, 1);
        if (messageHistoryResult.Error)
        {
            Debug.Log("Could not retrieve message history.");
            return;
        }

        // get the last message from the returned list
        var lastMessage = messageHistoryResult.Result.FirstOrDefault();

        if (lastMessage != null)
        {
            // add the "thumb up" emoji to the last message
            await lastMessage.ToggleReaction("\\u{1F44D}");
            Debug.Log("Added 'thumb up' reaction to the last message.");
        }
        else
        {
            Debug.Log("No messages found in the channel history.");
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
            Debug.Log("Support channel not found.");
            return;
        }
        var channel = channelResult.Result;
        Debug.Log($"Found channel with name {channel.Name}");

        // get the message history with the desired count
        var messageHistoryResult = await channel.GetMessageHistory(null, null, 1);
        if (messageHistoryResult.Error)
        {
            Debug.Log("Could not retrieve message history.");
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
                Debug.Log($"Reaction: {reaction.Value}");
            }
        }
        else
        {
            Debug.Log("No messages found in the channel history.");
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
            Debug.Log("Support channel not found.");
            return;
        }
        var channel = channelResult.Result;
        Debug.Log($"Found channel with name {channel.Name}");

        // get the message history with the desired count
        var messageHistoryResult = await channel.GetMessageHistory(null, null, 1);
        if (messageHistoryResult.Error)
        {
            Debug.Log("Could not retrieve message history.");
            return;
        }

        // get the last message from the returned list
        var lastMessage = messageHistoryResult.Result.FirstOrDefault();

        if (lastMessage != null)
        {
            // Check if the current user added the "thumb up" emoji to the last message
            if (lastMessage.HasUserReaction("\\u{1F44D}"))
            {
                Debug.Log("The current user has added a 'thumb up' reaction to the last message.");
            }
            else
            {
                Debug.Log("The current user has not added a 'thumb up' reaction to the last message.");
            }
        }
        else
        {
            Debug.Log("No messages found in the channel history.");
        }
        // snippet.end
    }
}
