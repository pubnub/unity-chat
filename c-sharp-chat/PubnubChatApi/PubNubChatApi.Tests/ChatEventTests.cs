using PubnubApi;
using PubnubChatApi;
using Channel = PubnubChatApi.Channel;

namespace PubNubChatApi.Tests;

[TestFixture]
public class ChatEventTests
{
    private Chat chat;
    private Channel channel;
    private User user;

    [SetUp]
    public async Task Setup()
    {
        chat = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(storeUserActivityTimestamp: true), new PNConfiguration(new UserId("event_tests_user"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        }));
        channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("event_tests_channel"));
        user = TestUtils.AssertOperation(await chat.GetCurrentUser());
        await channel.Join();
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
    public async Task TestModerationEvents()
    {
        var manualModerationEvent = new ManualResetEvent(false);
        user.OnModerationEvent += moderationEvent =>
        {
            Assert.True(moderationEvent.Payload.Contains("some_reason"));
            manualModerationEvent.Set();
        };
        user.SetListeningForModerationEvents(true);
        await Task.Delay(2500);
        await user.SetRestriction(channel.Id, new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "some_reason"
        });
        var moderationEventReceived = manualModerationEvent.WaitOne(8000);
        Assert.IsTrue(moderationEventReceived);
    }
}