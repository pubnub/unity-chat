using System.Diagnostics;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

[TestFixture]
public class UserTests
{
    private Chat chat;
    private Channel channel;
    private User user;

    [SetUp]
    public async Task Setup()
    {
        chat = await Chat.CreateInstance(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "user_tests_user", 
            storeUserActivityTimestamp: true)
        );
        channel = await chat.CreatePublicConversation("user_tests_channel");
        if (!chat.OLD_TryGetCurrentUser(out user))
        {
            Assert.Fail();
        }
        channel.Join();
        await Task.Delay(3500);
    }
    
    [TearDown]
    public async Task CleanUp()
    {
        channel.Leave();
        await Task.Delay(3000);
        chat.Destroy();
        await Task.Delay(3000);
    }

    [Test]
    public async Task TestUserActive()
    {
        await Task.Delay(500);
        Assert.True(user.Active);
    }
    
    [Test]
    public async Task TestLastUserActive()
    {
        await Task.Delay(500);
        var lastActive = user.LastActiveTimeStamp;
        Assert.False(string.IsNullOrEmpty(lastActive));
        Assert.True(long.TryParse(lastActive, out var numberTimeStamp));
        Assert.True(numberTimeStamp > 0);
    }

    [Test]
    public async Task TestUserUpdate()
    {
        var updatedReset = new ManualResetEvent(false);
        var testUser = await chat.GetOrCreateUser("wolololo_guy");

        await Task.Delay(5000);
        
        var newRandomUserName = Guid.NewGuid().ToString();
        testUser.OnUserUpdated += updatedUser =>
        {
            Assert.True(updatedUser.OLD_UserName == newRandomUserName);
            Assert.True(updatedUser.OLD_CustomData == "{\"some_key\":\"some_value\"}");
            Assert.True(updatedUser.OLD_Email == "some@guy.com");
            Assert.True(updatedUser.OLD_ExternalId == "xxx_some_guy_420_xxx");
            Assert.True(updatedUser.OLD_ProfileUrl == "www.some.guy");
            Assert.True(updatedUser.OLD_Status == "yes");
            Assert.True(updatedUser.OLD_DataType == "someType");
            updatedReset.Set();
        };
        testUser.SetListeningForUpdates(true);
        await Task.Delay(3000);
        await testUser.OLD_Update(new ChatUserData()
        {
            Username = newRandomUserName,
            CustomDataJson = "{\"some_key\":\"some_value\"}",
            Email = "some@guy.com",
            ExternalId = "xxx_some_guy_420_xxx",
            ProfileUrl = "www.some.guy",
            Status = "yes",
            Type = "someType"
        });
        var updated = updatedReset.WaitOne(15000);
        Assert.True(updated);
    }

    [Test]
    public async Task TestUserDelete()
    {
        var someUser = await chat.OLD_CreateUser(Guid.NewGuid().ToString());
        
        Assert.True(chat.OLD_TryGetUser(someUser.Id, out _), "Couldn't get freshly created user");

        await someUser.DeleteUser();

        await Task.Delay(3000);
        
        Assert.False(chat.OLD_TryGetUser(someUser.Id, out _), "Got the freshly deleted user");
    }

    [Test]
    public async Task TestUserWherePresent()
    {
        var someChannel = await chat.CreatePublicConversation();
        someChannel.Join();

        await Task.Delay(4000);

        var where = await user.WherePresent();
        
        Assert.Contains(someChannel.Id, where, "user.WherePresent() doesn't have most recently joined channel!");
    }
    
    [Test]
    public async Task TestUserIsPresentOn()
    {
        var someChannel = await chat.CreatePublicConversation();
        someChannel.Join();

        await Task.Delay(4000);

        var isOn = await user.IsPresentOn(someChannel.Id);
        
        Assert.True(isOn, "user.IsPresentOn() doesn't return true for most recently joined channel!");
    }
}