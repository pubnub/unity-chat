// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class UserSample
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
    
    public static async Task UserUpdatedEventExample()
    {
        // snippet.user_updated_event_example
        // Get a user
        var userResult = await chat.GetUser("user_id");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            
            user.OnUserUpdated += (user) =>
            {
                Debug.Log("User metadata updated!");
            };
        }
        // snippet.end
    }
}
