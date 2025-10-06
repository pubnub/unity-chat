// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class ListUserSample
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
    
    public static async Task GetUsersExample()
    {
        // snippet.get_users_example
        // fetch all existing users
        var usersWrapper = await chat.GetUsers();

        // check if users were successfully fetched
        if (!usersWrapper.Error)
        {
            Debug.Log("Existing user IDs:");

            // loop through the users and print their IDs
            foreach (var user in usersWrapper.Result.Users)
            {
                Debug.Log(user.Id);
            }
        }
        else
        {
            Debug.Log("No users found or unable to fetch users.");
        }
        // snippet.end
    }
    
    public static async Task PaginationExample()
    {
        // snippet.pagination_example
        // fetch the initial 25 users
        var initialUsers = await chat.GetUsers(limit: 25);
        if (initialUsers.Error)
        {
            Debug.Log("Couldn't fetch initial users!");
            return;
        }

        Debug.Log("Initial 25 users:");
        foreach (var user in initialUsers.Result.Users)
        {
            Debug.Log($"Id: {user.Id}, UserName: {user.UserName}, Status: {user.Status}");
        }

        // fetch the next set of users using the pagination token
        var nextUsers = await chat.GetUsers(limit: 25, page: initialUsers.Result.Page);
        if (nextUsers.Error)
        {
            Debug.Log("Couldn't fetch next users!");
            return;
        }
        
        Debug.Log("\nNext users:");
        foreach (var user in nextUsers.Result.Users)
        {
            Debug.Log($"Id: {user.Id}, UserName: {user.UserName}, Status: {user.Status}");
        }
        // snippet.end
    }
    
    public static async Task DeletedUsersExample()
    {
        // snippet.deleted_users_example
        var deletedUsers = await chat.GetUsers(filter: "Status='deleted'");
        if (deletedUsers.Error)
        {
            Debug.Log("Couldn't fetch deleted users!");
            return;
        }

        foreach (var user in deletedUsers.Result.Users)
        {
            Debug.Log($"Id: {user.Id}, UserName: {user.UserName}, Status: {user.Status}");
        }
        // snippet.end
    }
}
