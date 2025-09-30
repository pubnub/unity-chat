// snippet.using
using System;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class DeleteUserSample
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
    
    public static async Task DeleteUserUsingUserObjectExample()
    {
        // snippet.delete_user_using_user_object_example
        var userResult = await chat.GetUser("support_agent_15");
        if (userResult.Error)
        {
            Console.WriteLine("Couldn't find user!");
            return;
        }
        var user = userResult.Result;
        await user.DeleteUser();
        // snippet.end
    }
    
    public static async Task DeleteUserUsingChatObjectExample()
    {
        // snippet.delete_user_using_chat_object_example
        await chat.DeleteUser("support_agent_15");
        // snippet.end
    }
}
