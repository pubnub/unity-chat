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
            Console.WriteLine("Existing user IDs:");

            // loop through the users and print their IDs
            foreach (var user in usersWrapper.Result.Users)
            {
                Console.WriteLine(user.Id);
            }
        }
        else
        {
            Console.WriteLine("No users found or unable to fetch users.");
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
            Console.WriteLine("Couldn't fetch initial users!");
            return;
        }

        Console.WriteLine("Initial 25 users:");
        foreach (var user in initialUsers.Result.Users)
        {
            Console.WriteLine($"Id: {user.Id}, UserName: {user.UserName}, Status: {user.Status}");
        }

        // fetch the next set of users using the pagination token
        var nextUsers = await chat.GetUsers(limit: 25, page: initialUsers.Result.Page);
        if (nextUsers.Error)
        {
            Console.WriteLine("Couldn't fetch next users!");
            return;
        }
        
        Console.WriteLine("\nNext users:");
        foreach (var user in nextUsers.Result.Users)
        {
            Console.WriteLine($"Id: {user.Id}, UserName: {user.UserName}, Status: {user.Status}");
        }
        // snippet.end
    }
    
    public static async Task DeletedUsersExample()
    {
        // snippet.deleted_users_example
        var deletedUsers = await chat.GetUsers(filter: "Status='deleted'");
        if (deletedUsers.Error)
        {
            Console.WriteLine("Couldn't fetch deleted users!");
            return;
        }

        foreach (var user in deletedUsers.Result.Users)
        {
            Console.WriteLine($"Id: {user.Id}, UserName: {user.UserName}, Status: {user.Status}");
        }
        // snippet.end
    }
}
