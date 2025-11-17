// snippet.using
using System;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;
// snippet.end

public class MembershipChannelSample
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
    
    public static async Task GetMembershipsExample()
    {
        // snippet.get_memberships_example
        var userResult = await chat.GetUser("support_agent_15");
        if (userResult.Error)
        {
            Debug.Log("User not found.");
            return;
        }
        var user = userResult.Result;
        Debug.Log($"Found user with name {user.UserName}");
        
        // Get the memberships of the user
        var membershipsResult = await user.GetMemberships();

        if (!membershipsResult.Error)
        {
            Debug.Log($"Memberships of user {user.UserName}:");

            foreach (var membership in membershipsResult.Result.Memberships)
            {
                Debug.Log($"Channel ID: {membership.ChannelId}");
            }
        }
        // snippet.end
    }
    
    public static async Task GetMembershipUpdatesExample()
    {
        // snippet.get_membership_updates_example
        // reference the "support_agent_15" user
        var userResult = await chat.GetUser("support_agent_15");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            Debug.Log($"Found user with name {user.UserName}");
            
            // get the list of all user memberships
            var membershipsResponse = await user.GetMemberships();
            
            // extract the actual memberships from the response
            if (!membershipsResponse.Error)
            {
                var memberships = membershipsResponse.Result.Memberships;
                if (memberships.Any())
                {
                    // get the first membership
                    var firstMembership = memberships.First();
                    
                    // output the first membership details
                    Debug.Log($"First membership for user {user.UserName} is in channel {firstMembership.ChannelId}");
                    
                    // start listening for updates on memberships
                    firstMembership.StreamUpdates(true);
                    
                    // attach an event handler for membership updates
                    firstMembership.OnMembershipUpdated += OnMembershipUpdatedHandler;

                    // example event handler for membership updates
                    void OnMembershipUpdatedHandler(Membership updatedMembership)
                    {
                        Debug.Log($"Membership updated: {updatedMembership.ChannelId}");
                    }
                }
                else
                {
                    Debug.Log("The user 'support_agent_15' has no memberships.");
                }
            }
        }
        else
        {
            Debug.Log("User 'support_agent_15' not found.");
        }
        // snippet.end
    }
    
    public static async Task UpdateMembershipExample()
    {
        // snippet.update_membership_example
        // reference the "support_agent_15" user
        var userResult = await chat.GetUser("support_agent_15");
        if (userResult.Error)
        {
            Debug.Log("Couldn't find user!");
            return;
        }
        var user = userResult.Result;

        // get the list of all user memberships and filter out the right channel
        var membershipsWrapperResult = await user.GetMemberships(
            filter: "channel.id == 'high-priority-incidents'"
        );

        if(!membershipsWrapperResult.Error && membershipsWrapperResult.Result.Memberships.Any())
        {
            var membership = membershipsWrapperResult.Result.Memberships[0];
            membership.MembershipData.CustomData["role"] = "premium-support";
            // add custom metadata to the user membership
            await membership.Update(membership.MembershipData);
        }
        // snippet.end
    }
}
