using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

public class ChatEventTests
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
            "event_tests_user")
        );
        channel = await chat.CreatePublicConversation("event_tests_channel");
        if (!chat.TryGetCurrentUser(out user))
        {
            Assert.Fail();
        }
        await channel.Join();
    }
    
    [Test]
    public async Task TestModerationEvents()
    {
        var manualModerationEvent = new ManualResetEvent(false);
        chat.OnModerationEvent += moderationEvent =>
        {
            Assert.True(moderationEvent.Payload.Contains("some_reason"));
            manualModerationEvent.Set();
        };
        chat.StartListeningForModerationEvents(user.Id);
        await user.SetRestriction(channel.Id, new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "some_reason"
        });
        var moderationEventReceived = manualModerationEvent.WaitOne(5000);
        Assert.IsTrue(moderationEventReceived);
    }
}