using PubnubChatApi;

namespace PubNubChatApi.Tests;

public static class TestUtils
{
    public static async Task<User> GetOrCreateUser(this Chat chat, string userId, ChatUserData? userData = null)
    {
        var getUser = await chat.GetUser(userId);
        if (getUser.Error)
        {
            userData ??= new ChatUserData();
            var createUser = await chat.CreateUser(userId, userData);
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

    public static void AssertOperation(ChatOperationResult chatOperationResult)
    {
        if (chatOperationResult.Error)
        {
            Assert.Fail($"Chat operation failed! Error: {chatOperationResult.Exception.Message}");
        }
    }
    public static T AssertOperation<T>(ChatOperationResult<T> chatOperationResult)
    {
        if (chatOperationResult.Error)
        {
            Assert.Fail($"Chat operation for getting {typeof(T).Name} failed! Error: {chatOperationResult.Exception.Message}");
        }
        return chatOperationResult.Result;
    }
}