// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

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
            Debug.Log("Couldn't find user!");
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
    
    public static async Task RestoreUserSample()
    {
        // snippet.restore_user_sample
        var userResult = await chat.GetUser("support_agent_15");
        if (userResult.Error)
        {
            Debug.Log("User to restore doesn't exist.");
            return;
        }
        var user = userResult.Result;
        var restoreResult = await user.Restore();
        //This could happen because the user was not soft deleted
        if (restoreResult.Error)
        {
            Debug.LogError($"An error has occured when trying to restore user: {restoreResult.Exception.Message}");
            return;
        }
        // snippet.end
    }
    
    public static async Task SoftDeleteSample()
    {
        // snippet.user_soft_delete
        // using User object
        var userResult = await chat.GetUser("support-agent-15");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            var softDeleteResult = await user.DeleteUser(soft: true);
            //Could be for example because it was already soft deleted
            if (softDeleteResult.Error)
            {
                Debug.LogError($"Error when trying to soft delete user: {softDeleteResult.Exception.Message}");
            }
        }

        // or using Chat object
        var softDeleteFromChat = await chat.DeleteUser("support-agent-15", soft: true);
        //Same as above, could be because it was already soft deleted
        if (softDeleteFromChat.Error)
        {
            Debug.LogError($"Error when trying to soft delete user: {softDeleteFromChat.Exception.Message}");
        }
        // snippet.end
    }
}
