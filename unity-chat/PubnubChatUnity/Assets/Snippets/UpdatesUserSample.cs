// snippet.using
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class UpdatesUserSample
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
    
    public static async Task UpdateUserUsingUserObjectExample()
    {
        // snippet.update_user_using_user_object_example
        var userResult = await chat.GetUser("user_id");
        if (userResult.Error)
        {
            Console.WriteLine("Couldn't find user!");
            return;
        }
        var user = userResult.Result;
        var updatedUserData = new ChatUserData
        {
            ProfileUrl = "https://www.linkedin.com/mkelly_vp2"
        };
        await user.Update(updatedUserData);
        // snippet.end
    }
    
    public static async Task UpdateUserUsingChatObjectExample()
    {
        // snippet.update_user_using_chat_object_example
        var updatedUserData = new ChatUserData
        {
            ProfileUrl = "https://www.linkedin.com/mkelly_vp2"
        };
        await chat.UpdateUser("support_agent_15", updatedUserData);
        // snippet.end
    }
    
    public static async Task SetListeningForUpdatesExample()
    {
        // snippet.set_listening_for_updates_example
        var userResult = await chat.GetUser("support_agent_15");
        if (userResult.Error)
        {
            Console.WriteLine("Couldn't find user!");
            return;
        }
        var user = userResult.Result;
        
        user.SetListeningForUpdates(true);
      
        user.OnUserUpdated += OnUserUpdatedHandler; // or use lambda
        void OnUserUpdatedHandler(User user)
        {
            Console.WriteLine($"User updated: {user.Id}");
        }
        // snippet.end
    }
    
    public static async Task AddListenerToUsersUpdateExample()
    {
        // snippet.add_listener_to_users_update_example
        List<string> users = new List<string> { "support_agent_15", "support-manager" };
        Action<User> listener = (User user) => 
        {
            // Print the updated user name
            Console.WriteLine("Updated user Name: " + user.UserName);
        };
        chat.AddListenerToUsersUpdate(users, listener);
        // snippet.end
    }
}
