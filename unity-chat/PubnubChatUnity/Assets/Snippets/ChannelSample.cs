// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class ChannelSample
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
    
    public static async Task EventSubscriptionExample()
    {
        // snippet.event_subscription_example
        // Get or create a channel
        var channelResult = await chat.GetChannel("my_channel");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            
            channel.OnMessageReceived += (message) =>
            {
                Debug.Log("New message received!");
            };
        }
        // snippet.end
    }
    
    public static async Task GetInviteesExample()
    {
        // snippet.get_invitees_example
        // Get or create a channel
        var channelResult = await chat.GetChannel("my_channel");
        if (channelResult.Error)
        {
            Debug.LogError($"Could not fetch channel! Error: {channelResult.Exception.Message}");
        }
        var channel = channelResult.Result;
        var getInvitees = await channel.GetInvitees();
        if (getInvitees.Error)
        {
            Debug.LogError($"Could not fetch invitees! Error: {getInvitees.Exception.Message}");
        }
        foreach (var membership in getInvitees.Result.Memberships)
        {
            Debug.Log($"User {membership.UserId} has is invited to channel {membership.ChannelId}");
        }
        // snippet.end
    }
}
