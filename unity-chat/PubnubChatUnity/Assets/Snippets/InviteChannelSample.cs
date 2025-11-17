// snippet.using
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class InviteChannelSample
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
    
    public static async Task InviteOneUserExample()
    {
        // snippet.invite_one_user_example
        // reference "support-agent-15"
        var userResult = await chat.GetUser("support-agent-15");
        if (userResult.Error)
        {
            Debug.Log("Couldn't find user to invite!");
            return;
        }
        var user = userResult.Result;

        // get the channel
        var channelResult = await chat.GetChannel("high-prio-incidents");
        if (channelResult.Error)
        {
            Debug.Log("Couldn't find channel to invite!");
            return;
        }
        var channel = channelResult.Result;

        // invite the agent to join the channel
        await channel.Invite(user);
        // snippet.end
    }
    
    public static async Task InviteMultipleUsersExample()
    {
        // snippet.invite_multiple_users_example
        // reference "support-agent-15"
        var user1Result = await chat.GetUser("support-agent-15");
        if (user1Result.Error)
        {
            Debug.Log("Couldn't find first user!");
            return;
        }
        var user1 = user1Result.Result;

        // reference "support-agent-16"
        var user2Result = await chat.GetUser("support-agent-16");
        if (user2Result.Error)
        {
            Debug.Log("Couldn't find second user!");
            return;
        }
        var user2 = user2Result.Result;

        // reference the "high-prio-incidents" channel
        var channelResult = await chat.GetChannel("high-prio-incidents");
        if (channelResult.Error)
        {
            Debug.Log("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        // invite both agents to join the channel
        var newMemberships = await channel.InviteMultiple(new List<User> { user1, user2 });
        // snippet.end
    }
    
    public static async Task ListenToInviteEventsExample()
    {
        // snippet.listen_to_invite_events_example
        var userResult = await chat.GetUser("support-agent-2");
        if(userResult.Error){
            Debug.Log("Couldn't find user!");
            return;
        }
        var user = userResult.Result;

        //start listening
        user.StreamInviteEvents(true);
        //lambda event handler
        user.OnInviteEvent += (inviteEvent) => 
            {
                if(inviteEvent.ChannelId == "support" && inviteEvent.UserId == "support-agent-2")
                {
                    Debug.Log("User support-agent-2 has been invited to the support channel!");
                }    
            };
        // snippet.end
    }
}
