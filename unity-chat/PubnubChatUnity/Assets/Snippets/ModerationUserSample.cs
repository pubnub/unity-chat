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

public class ModerationUserSample
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
    
    public static async Task MuteUserChatObjectExample()
    {
        // snippet.mute_user_chat_object_example
        await chat.SetRestriction(
            userId: "support_agent_15",
            channelId: "support",
            restriction: new Restriction()
            {
                Ban = false,
                Mute = true,
                Reason = string.Empty
            }
        );
        // snippet.end
    }
    
    public static async Task MuteUserUserObjectExample()
    {
        // snippet.mute_user_user_object_example
        var userResult = await chat.GetUser("support_agent_15");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            await user.SetRestriction(
                "support",
                new Restriction()
                {
                    Ban = false,
                    Mute = true,
                    Reason = string.Empty
                }
            );
        }
        // snippet.end
    }
    
    public static async Task MuteUserChannelObjectExample()
    {
        // snippet.mute_user_channel_object_example
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            await channel.SetRestrictions(
                "support_agent_15",
                new Restriction()
                {
                    Ban = false,
                    Mute = true,
                    Reason = string.Empty
                }
            );
        }
        // snippet.end
    }
    
    public static async Task BanUserChatObjectExample()
    {
        // snippet.ban_user_chat_object_example
        await chat.SetRestriction(
            "support_agent_15",
            "support",
            new Restriction()
            {
                Ban = true,
                Mute = false,
                Reason = "Violated community guidelines"
            }
        );
        // snippet.end
    }
    
    public static async Task BanUserUserObjectExample()
    {
        // snippet.ban_user_user_object_example
        var userResult = await chat.GetUser("support_agent_15");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            await user.SetRestriction(
                "support",
                new Restriction()
                {
                    Ban = true,
                    Mute = false,
                    Reason = "Violated community guidelines"
                }
            );
        }
        // snippet.end
    }
    
    public static async Task BanUserChannelObjectExample()
    {
        // snippet.ban_user_channel_object_example
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            await channel.SetRestrictions(
                "support_agent_15",
                new Restriction()
                {
                    Ban = true,
                    Mute = false,
                    Reason = "Violated community guidelines"
                }
            );
        }
        // snippet.end
    }
    
    public static async Task GetChannelRestrictionsExample()
    {
        // snippet.get_channel_restrictions_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        var userResult = await chat.GetUser("support_agent_15");
        if (userResult.Error)
        {
            Console.WriteLine("Couldn't find user!");
            return;
        }
        var user = userResult.Result;

        // check user restrictions
        var restrictionResult = await user.GetChannelRestrictions(channel);
        var restriction = restrictionResult.Result;
        // snippet.end
    }
    
    public static async Task GetUserRestrictionsExample()
    {
        // snippet.get_user_restrictions_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        var userResult = await chat.GetUser("support_agent_15");
        if (userResult.Error)
        {
            Console.WriteLine("Couldn't find user!");
            return;
        }
        var restrictedUser = userResult.Result;

        var restrictionResult = await channel.GetUserRestrictions(restrictedUser);
        var restriction = restrictionResult.Result;
        // snippet.end
    }
    
    public static async Task GetChannelsRestrictionsExample()
    {
        // snippet.get_channels_restrictions_example
        var userResult = await chat.GetUser("support_agent_15");
        if(userResult.Error){
            return;    
        }
        var user = userResult.Result;
        var restrictionsWrapperResult = await user.GetChannelsRestrictions();
        if (!restrictionsWrapperResult.Error)
        {
            foreach(var restriction in restrictionsWrapperResult.Result.Restrictions)
            {
                Console.WriteLine($"Channel: {restriction.ChannelId}, Ban: {restriction.Ban}, Mute: {restriction.Mute}");
            }
        }
        // snippet.end
    }
    
    public static async Task GetUsersRestrictionsExample()
    {
        // snippet.get_users_restrictions_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error) return;
        var channel = channelResult.Result;
        
        var restrictionsWrapperResult = await channel.GetUsersRestrictions();
        if (!restrictionsWrapperResult.Error)
        {
            foreach(var userRestriction in restrictionsWrapperResult.Result.Restrictions)
            {
                Console.WriteLine(
                            $"User: {userRestriction.UserId}, " +
                            $"Banned: {userRestriction.Ban}, " +
                            $"Muted: {userRestriction.Mute}, " +
                            $"Reason: {userRestriction.Reason}");
            }
        }
        // snippet.end
    }
}
