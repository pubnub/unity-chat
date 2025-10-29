using PubnubApi;
using PubnubChatApi;
using Channel = PubnubChatApi.Channel;

namespace PubNubChatApi.Tests;

public class ClientSideMuteTests
{
    private Chat chat1;
    private User user1;
    
    private Chat chat2;
    private User user2;
    
    private Channel channel1;
    private Channel channel2;

    [SetUp]
    public async Task Setup()
    {
        chat1 = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(), 
            new PNConfiguration(new UserId("client_side_mute_test_user_1"))
            {
                PublishKey = PubnubTestsParameters.PublishKey,
                SubscribeKey = PubnubTestsParameters.SubscribeKey
            }));
        chat2 = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(), 
            new PNConfiguration(new UserId("client_side_mute_test_user_2"))
            {
                PublishKey = PubnubTestsParameters.PublishKey,
                SubscribeKey = PubnubTestsParameters.SubscribeKey
            }));
        user1 = TestUtils.AssertOperation(await chat1.GetCurrentUser());
        user2 = TestUtils.AssertOperation(await chat2.GetCurrentUser());
        channel1 = TestUtils.AssertOperation(await chat1.CreatePublicConversation("mute_tests_channel"));
        await Task.Delay(3000);
        channel2 = TestUtils.AssertOperation(await chat2.GetChannel("mute_tests_channel"));
    }
    
    [TearDown]
    public async Task CleanUp()
    {
        await channel1.Leave();
        await channel2.Leave();
        await user1.DeleteUser(false);
        chat1.Destroy();
        await user2.DeleteUser(false);
        chat2.Destroy();
        await channel1.Delete(false);
        await channel2.Delete(false);
        await Task.Delay(4000);
    }
    
    [Test]
    public async Task TestMuteInMessages()
    {
        var messageReset = new ManualResetEvent(false);
        channel1.OnMessageReceived += message =>
        {
            messageReset.Set();
        };
        await channel1.Join();

        await Task.Delay(3000);
        
        await channel2.SendText("This message should not be muted.");
        var received = messageReset.WaitOne(10000);
        Assert.True(received, "Didn't receive message from not-yet-muted user.");

        messageReset = new ManualResetEvent(false);
        await chat1.MutedUsersManager.MuteUser(user2.Id);
        await channel2.SendText("This message should be muted.");
        received = messageReset.WaitOne(10000);
        Assert.False(received, "Received message from muted user.");
        
        messageReset = new ManualResetEvent(false);
        await chat1.MutedUsersManager.UnMuteUser(user2.Id);
        await channel2.SendText("This message shouldn't be muted now.");
        received = messageReset.WaitOne(10000);
        Assert.True(received, "Didn't receive message from un-muted user.");
    }
    
    [Test]
    public async Task TestMuteInMessageHistory()
    {
        await channel2.SendText("One");
        await channel2.SendText("Two");
        await channel2.SendText("Three");

        await Task.Delay(6000);

        var history = TestUtils.AssertOperation(await channel1.GetMessageHistory("99999999999999999", "00000000000000000", 3));
        Assert.True(history.Count == 3, "Didn't get message history for non-muted user");

        await chat1.MutedUsersManager.MuteUser(user2.Id);
        
        history = TestUtils.AssertOperation(await channel1.GetMessageHistory("99999999999999999", "00000000000000000", 3));
        Assert.True(history.Count == 0, "Got message history for muted user");
        
        await chat1.MutedUsersManager.UnMuteUser(user2.Id);
        
        history = TestUtils.AssertOperation(await channel1.GetMessageHistory("99999999999999999", "00000000000000000", 3));
        Assert.True(history.Count == 3, "Didn't get message history for un-muted user");
    }
    
    [Test]
    public async Task TestMuteInEvents()
    {
        var eventReset = new ManualResetEvent(false);
        channel1.OnCustomEvent += chatEvent =>
        {
            if (chatEvent.Type != PubnubChatEventType.Custom)
            {
                return;
            }
            eventReset.Set();
        };
        channel1.SetListeningForCustomEvents(true);

        await Task.Delay(3000);
        
        await chat2.EmitEvent(PubnubChatEventType.Custom, channel2.Id, "{\"test\":\"not-muted\"}");
        var received = eventReset.WaitOne(10000);
        Assert.True(received, "Didn't receive event from not-yet-muted user.");

        eventReset = new ManualResetEvent(false);
        await chat1.MutedUsersManager.MuteUser(user2.Id);
        await chat2.EmitEvent(PubnubChatEventType.Custom, channel2.Id, "{\"test\":\"muted\"}");
        received = eventReset.WaitOne(10000);
        Assert.False(received, "Received event from muted user.");
        
        eventReset = new ManualResetEvent(false);
        await chat1.MutedUsersManager.UnMuteUser(user2.Id);
        await chat2.EmitEvent(PubnubChatEventType.Custom, channel2.Id, "{\"test\":\"un-muted\"}");
        received = eventReset.WaitOne(10000);
        Assert.True(received, "Didn't receive event from un-muted user.");
    }
    
    [Test]
    public async Task TestMuteInEventsHistory()
    {
        await chat2.EmitEvent(PubnubChatEventType.Custom, channel2.Id, "{\"test\":\"one\"}");
        await chat2.EmitEvent(PubnubChatEventType.Custom, channel2.Id, "{\"test\":\"two\"}");
        await chat2.EmitEvent(PubnubChatEventType.Custom, channel2.Id, "{\"test\":\"three\"}");

        var history = TestUtils.AssertOperation(await chat1.GetEventsHistory(channel1.Id,"99999999999999999", "00000000000000000", 3));
        Assert.True(history.Events.Count == 3, "Didn't get events history for non-muted user");

        await chat1.MutedUsersManager.MuteUser(user2.Id);
        
        history = TestUtils.AssertOperation(await chat1.GetEventsHistory(channel1.Id,"99999999999999999", "00000000000000000", 3));
        Assert.True(history.Events.Count == 0, "Got events history for muted user");
        
        await chat1.MutedUsersManager.UnMuteUser(user2.Id);
        
        history = TestUtils.AssertOperation(await chat1.GetEventsHistory(channel1.Id,"99999999999999999", "00000000000000000", 3));
        Assert.True(history.Events.Count == 3, "Didn't get events history for un-muted user");
    }
    
    [Test]
    public async Task TestMuteListSyncing()
    {
        var userId = Guid.NewGuid().ToString();
        var chatWithSync = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(syncMutedUsers:true), 
            new PNConfiguration(new UserId(userId))
            {
                PublishKey = PubnubTestsParameters.PublishKey,
                SubscribeKey = PubnubTestsParameters.SubscribeKey
            }));
        TestUtils.AssertOperation(await chatWithSync.MutedUsersManager.MuteUser(user1.Id));
        
        chatWithSync.Destroy();

        await Task.Delay(3000);
        var chatWithSyncSecondInstance = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(syncMutedUsers:true), 
            new PNConfiguration(new UserId(userId))
            {
                PublishKey = PubnubTestsParameters.PublishKey,
                SubscribeKey = PubnubTestsParameters.SubscribeKey
            }));
        await Task.Delay(5000);
        Assert.True(chatWithSyncSecondInstance.MutedUsersManager.MutedUsers.Contains(user1.Id), "Second instance of chat didn't have synced mute list");
        
        chatWithSyncSecondInstance.Destroy();
        await chatWithSyncSecondInstance.DeleteUser(userId);
    }
}