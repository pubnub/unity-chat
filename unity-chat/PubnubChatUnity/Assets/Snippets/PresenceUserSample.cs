// snippet.using
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class PresenceUserSample
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
    
    public static async Task WherePresentUserObjectExample()
    {
        // snippet.where_present_user_object_example
        var userResult = await chat.GetUser("support_agent_15");
        if (userResult.Error)
        {
            Console.WriteLine("Couldn't find user!");
            return;
        }
        var user = userResult.Result;

        var channelIdsResult = await user.WherePresent();
        if (!channelIdsResult.Error)
        {
            var channelIds = channelIdsResult.Result;
        }
        // snippet.end
    }
    
    public static async Task WherePresentChatObjectExample()
    {
        // snippet.where_present_chat_object_example
        // reference the "chat" object and invoke the "wherePresent()" method.
        var channelIdsResult = await chat.WherePresent("support_agent_15");
        if (!channelIdsResult.Error)
        {
            var channelIds = channelIdsResult.Result;
        }
        // snippet.end
    }
    
    public static async Task IsPresentOnUserObjectExample()
    {
        // snippet.is_present_on_user_object_example
        var userResult = await chat.GetUser("support_agent_15");
        if (userResult.Error)
        {
            Console.WriteLine("Couldn't find user!");
            return;
        }
        var user = userResult.Result;

        var isPresentOnResult = await user.IsPresentOn("support");
        if (!isPresentOnResult.Error)
        {
            var isPresentOn = isPresentOnResult.Result;
        }
        // snippet.end
    }
    
    public static async Task IsUserPresentChannelObjectExample()
    {
        // snippet.is_user_present_channel_object_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        var isPresentResult = await channel.IsUserPresent("support_agent_15");
        if (!isPresentResult.Error)
        {
            var isPresent = isPresentResult.Result;
        }
        // snippet.end
    }
    
    public static async Task IsPresentChatObjectExample()
    {
        // snippet.is_present_chat_object_example
        var isPresentResult = await chat.IsPresent("support_agent_15", "support");
        if (!isPresentResult.Error)
        {
            var isPresent = isPresentResult.Result;
        }
        // snippet.end
    }
    
    public static async Task WhoIsPresentChannelObjectExample()
    {
        // snippet.who_is_present_channel_object_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        var userIdsResult = await channel.WhoIsPresent();
        if (!userIdsResult.Error)
        {
            var userIds = userIdsResult.Result;
        }
        // snippet.end
    }
    
    public static async Task WhoIsPresentChatObjectExample()
    {
        // snippet.who_is_present_chat_object_example
        var userIdsResult = await chat.WhoIsPresent("support");
        if (!userIdsResult.Error)
        {
            var userIds = userIdsResult.Result;
        }
        // snippet.end
    }
    
    public static async Task OnPresenceUpdateExample()
    {
        // snippet.on_presence_update_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        channel.OnPresenceUpdate += OnPresenceUpdateHandler; // or use lambda

        void OnPresenceUpdateHandler(List<string> users)
        {
            Console.WriteLine($"Users present: {string.Join(", ", users)}");
        }
        // snippet.end
    }
    
    public static async Task CheckUserActiveStatusExample()
    {
        // snippet.check_user_active_status_example
        var userResult = await chat.GetUser("support_agent_15");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            Console.WriteLine($"Is user active?: {user.Active}");
        }
        else 
        {
            Console.WriteLine("User not found.");
        }
        // snippet.end
    }
    
    public static async Task GetLastActiveTimestampExample()
    {
        // snippet.get_last_active_timestamp_example
        // Get the user's last active timestamp
        var userResult = await chat.GetUser("support_agent_15");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            Console.WriteLine($"User last active timestamp: {user.LastActiveTimeStamp}");
        }
        else 
        {
            Console.WriteLine("User not found.");
        }
        // snippet.end
    }
}
