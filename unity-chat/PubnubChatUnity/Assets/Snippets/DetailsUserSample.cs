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

public class DetailsUserSample
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
    
    public static async Task GetUserDetailsExample()
    {
        // snippet.get_user_details_example
        var result = await chat.GetUser("support_agent_15");
        if (!result.Error)
        {
            var user = result.Result;
            Console.WriteLine($"Found user with name {user.UserName}");
        }
        // snippet.end
    }
    
    public static async Task GetCurrentUserExample()
    {
        // snippet.get_current_user_example
        var result = await chat.GetCurrentUser();
        if (!result.Error)
        {
            var user = result.Result;
            Console.WriteLine($"Current user is {user.UserName}");
            
            // perform additional actions with the user if needed
        }
        else
        {
            Console.WriteLine("Current user not found.");
        }
        // snippet.end
    }
}
