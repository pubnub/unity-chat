using PubNubChatAPI.Entities;

namespace PubNubChatApi.Tests;

public static class TestUtils
{
    public static User GetOrCreateUser(this Chat chat, string userId)
    {
        if (chat.TryGetUser(userId, out var user))
        {
            return user;
        }
        else
        {
            return chat.CreateUser(userId);
        }
    }
}