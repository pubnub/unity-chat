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

public class MembershipSample
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
    
    public static async Task MembershipUpdatedEventExample()
    {
        // snippet.membership_updated_event_example
        // Get user memberships
        var membershipsResult = await chat.GetUserMemberships("myUniqueUserId");
        if (!membershipsResult.Error && membershipsResult.Result.Memberships.Count > 0)
        {
            var membership = membershipsResult.Result.Memberships[0];
            
            membership.OnMembershipUpdated += (membership) =>
            {
                Console.WriteLine("Membership metadata updated!");
            };
        }
        // snippet.end
    }
}
