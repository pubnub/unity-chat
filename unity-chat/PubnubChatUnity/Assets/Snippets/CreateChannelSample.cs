// snippet.using
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class CreateChannelSample
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
    
    public static async Task CreateDirectConversationExample()
    {
        // snippet.create_direct_conversation_example
        var user = await chat.GetUser("agent-007");
        if (user.Error)
        {
            Console.WriteLine("Couldn't find user!");
            return;
        }

        string channelId = "direct.agent-001&agent-007"; 
        ChatChannelData channelData = new ChatChannelData
        {
            Name = "Customer XYZ Discussion",
            Description = "Conversation about customer XYZ",
            CustomData = new Dictionary<string, object>(),
            Status = "active",
            Type = "direct"    
        };

        // Call the method to create the direct conversation
        var result = await chat.CreateDirectConversation(user.Result, channelId, channelData);
        // snippet.end
    }
    
    public static async Task CreateGroupConversationExample()
    {
        // snippet.create_group_conversation_example
        // reference both agents you want to talk to
        var user1Result = await chat.GetUser("agent-007");
        if (user1Result.Error)
        {
            Console.WriteLine("Couldn't find first user!");
            return;
        }
        var user2Result = await chat.GetUser("agent-008");
        if (user2Result.Error)
        {
            Console.WriteLine("Couldn't find second user!");
            return;
        }

        List<User> users = new List<User> { user1Result.Result, user2Result.Result };

        // Define the channel's ID and details via ChatChannelData
        string channelId = "group-chat-1";  // Optional, can be auto-generated
        ChatChannelData channelData = new ChatChannelData
        {
            Name = "Weekly syncs on customer XYZ",
            Description = "Discussion about customer XYZ",
            CustomData = new Dictionary<string, object>() { { "purpose", "premium-support" } },
            Status = "active", 
            Type = "group" 
        };

        // Call the method to create the group conversation
        var result = await chat.CreateGroupConversation(users, channelId, channelData);
        // snippet.end
    }
    
    public static async Task CreatePublicConversationExample()
    {
        // snippet.create_public_conversation_example
        ChatChannelData additionalData = new ChatChannelData
        {
            Name = "Support channel",
            Description = "Discussion about support for all suers",
            Status = "active", 
            Type = "public" 
        };

        var publicConversation = await chat.CreatePublicConversation("ask-support", additionalData);
        // snippet.end
    }
}
