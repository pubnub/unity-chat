using PubnubApi;
using PubnubChatApi;
using Channel = PubnubChatApi.Channel;

namespace PubNubChatApi.Tests;

[TestFixture]
public class PushTests
{
    private Chat chat;
    private Channel channel;
    private User user;

    [SetUp]
    public async Task Setup()
    {
        chat = TestUtils.AssertOperation(await Chat.CreateInstance(
            new PubnubChatConfig(pushNotifications: new PubnubChatConfig.PushNotificationsConfig()
            {
                APNSEnvironment = PushEnvironment.Development,
                APNSTopic = "someTopic",
                DeviceGateway = PNPushType.FCM,
                DeviceToken = "sometoken",
                SendPushes = true
            }),
            new PNConfiguration(new UserId("push_tests_user"))
            {
                PublishKey = PubnubTestsParameters.PublishKey,
                SubscribeKey = PubnubTestsParameters.SubscribeKey,
            }));
        channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("push_tests_channel",
            new ChatChannelData() { Name = "Push Channels Name" }));
        user = TestUtils.AssertOperation(await chat.GetCurrentUser());
        await channel.Join();
        await Task.Delay(3500);
    }

    [TearDown]
    public async Task CleanUp()
    {
        await channel.Leave();
        await Task.Delay(3000);
        chat.Destroy();
        await Task.Delay(3000);
    }

    [Test]
    public async Task TestAddAndRemovePushChannel()
    {
        var res = await channel.RegisterForPush();
        TestUtils.AssertOperation(res);
        await Task.Delay(2000);
        var pushChannels = TestUtils.AssertOperation(await chat.GetPushChannels());
        Assert.True(pushChannels.Contains(channel.Id), "Push channels don't contain registered channel ID");
        TestUtils.AssertOperation(await channel.UnRegisterFromPush());
        await Task.Delay(2000);
        pushChannels = TestUtils.AssertOperation(await chat.GetPushChannels());
        Assert.False(pushChannels.Contains(channel.Id), "Push channels contain unregistered channel ID");
    }

    [Test]
    public async Task TestRemoveAllPushChannel()
    {
        TestUtils.AssertOperation(await channel.RegisterForPush());
        await Task.Delay(2000);
        var pushChannels = TestUtils.AssertOperation(await chat.GetPushChannels());
        Assert.True(pushChannels.Contains(channel.Id), "Push channels don't contain registered channel ID");
        TestUtils.AssertOperation(await chat.UnRegisterAllPushChannels());
        pushChannels = TestUtils.AssertOperation(await chat.GetPushChannels());
        Assert.False(pushChannels.Contains(channel.Id), "Push channels contain unregistered channel ID");
    }

    [Test]
    public async Task TestPublishWithPushData()
    {
        TestUtils.AssertOperation(await channel.SendText("some_message",
            new SendTextParams()
                { CustomPushData = new Dictionary<string, string>() { { "some_key", "some_value" } } }));
    }
}