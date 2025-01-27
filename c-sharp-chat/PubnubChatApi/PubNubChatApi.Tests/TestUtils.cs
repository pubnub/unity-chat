using PubNubChatAPI.Entities;

namespace PubNubChatApi.Tests;

public static class TestUtils
{
    public static async Task<User> GetOrCreateUser(this Chat chat, string userId)
    {
        if (chat.TryGetUser(userId, out var user))
        {
            return user;
        }
        else
        {
            return await chat.CreateUser(userId);
        }
    }
}