using System.Diagnostics;
using PubnubApi;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using Channel = PubNubChatAPI.Entities.Channel;

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
        chat = new Chat(new PubnubChatConfig(storeUserActivityTimestamp: true), new PNConfiguration(new UserId("user_tests_user"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        });
        channel = await chat.CreatePublicConversation("user_tests_channel");
        if (!chat.TryGetCurrentUser(out user))
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
            Assert.True(updatedUser.UserName == newRandomUserName);
            Assert.True(updatedUser.CustomData.TryGetValue("some_key", out var value) && value.ToString() == "some_value");
            Assert.True(updatedUser.Email == "some@guy.com");
            Assert.True(updatedUser.ExternalId == "xxx_some_guy_420_xxx");
            Assert.True(updatedUser.ProfileUrl == "www.some.guy");
            Assert.True(updatedUser.Status == "yes");
            Assert.True(updatedUser.DataType == "someType");
            updatedReset.Set();
        };
        testUser.SetListeningForUpdates(true);
        await Task.Delay(3000);
        await testUser.Update(new ChatUserData()
        {
            Username = newRandomUserName,
            CustomData = new Dictionary<string, object>()
            {
                {"some_key", "some_value"}
            },
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
        var someUser = await chat.CreateUser(Guid.NewGuid().ToString());
        
        Assert.True(chat.TryGetUser(someUser.Id, out _), "Couldn't get freshly created user");

        await someUser.DeleteUser();

        await Task.Delay(3000);
        
        Assert.False(chat.TryGetUser(someUser.Id, out _), "Got the freshly deleted user");
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