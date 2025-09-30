// snippet.using
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubApi.Unity;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using PubnubChat.Runtime;
using UnityEngine;
// snippet.end

public class CreateUserSample
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
    
    public static async Task CreateUserExample()
    {
        // snippet.create_user_example
        // Define the custom data for the user
        var userData = new ChatUserData
        {
            Username = "Support Agent",
            ProfileUrl = "https://example.com/avatar.png",
            Email = "agent@example.com",
            CustomData = new Dictionary<string, object>
            {
                { "title", "Customer Support Agent" },
                { "linkedin_profile", "https://www.linkedin.com/in/support-agent" }
            },
            Status = "active",
            Type = "support"
        };

        // Create the user with the specified ID and custom data
        var result = await chat.CreateUser("support_agent_15", userData);
        var user = result.Result;
        // snippet.end
    }
}
