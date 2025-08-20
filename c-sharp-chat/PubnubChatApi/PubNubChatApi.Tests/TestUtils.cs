using PubNubChatAPI.Entities;

namespace PubNubChatApi.Tests;

public static class TestUtils
{
    public static async Task<User> GetOrCreateUser(this Chat chat, string userId)
    {
        var getUser = await chat.GetUser(userId);
        if (getUser.Error)
        {
            var createUser = await chat.CreateUser(userId);
            if (createUser.Error)
            {
                Assert.Fail($"Failed to create User! Error: {createUser.Exception.Message}");
            }else
            {
                return createUser.Result;
            }
        }
        return getUser.Result;
    }
}