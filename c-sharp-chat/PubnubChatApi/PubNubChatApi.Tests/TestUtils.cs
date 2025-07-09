using PubNubChatAPI.Entities;

namespace PubNubChatApi.Tests;

public static class TestUtils
{
    public static async Task<User> GetOrCreateUser(this Chat chat, string userId)
    {
        if (chat.OLD_TryGetUser(userId, out var user))
        {
            return user;
        }
        else
        {
            return await chat.OLD_CreateUser(userId);
        }
    }
}