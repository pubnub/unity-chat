using System.Diagnostics;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

public class UserTests
{
    private Chat chat;
    private Channel channel;
    private User user;

    [SetUp]
    public void Setup()
    {
        chat = new Chat(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "user_tests_user", 
            storeUserActivityTimestamp: true)
        );
        channel = chat.CreatePublicConversation("user_tests_channel");
        if (!chat.TryGetCurrentUser(out user))
        {
            Assert.Fail();
        }
        channel.Join();
    }

    [Test]
    public async Task TestUserActive()
    {
        await Task.Delay(500);
        Assert.True(user.Active);
    }

    [Test]
    public async Task TestUserUpdate()
    {
        var updatedReset = new ManualResetEvent(false);
        var testUser = chat.CreateUser("wolololo");

        await Task.Delay(5000);
        
        var newRandomUserName = Guid.NewGuid().ToString();
        testUser.OnUserUpdated += updatedUser =>
        {
            Assert.True(updatedUser.UserName == newRandomUserName);
            updatedReset.Set();
        };
        testUser.StartListeningForUpdates();
        testUser.Update(new ChatUserData()
        {
            Username = newRandomUserName
        });
        var updated = updatedReset.WaitOne(8000);
        Assert.True(updated);
    }
}