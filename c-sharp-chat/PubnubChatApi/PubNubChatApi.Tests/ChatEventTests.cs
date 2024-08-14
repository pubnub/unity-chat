using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

public class ChatEventTests
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
            "event_tests_user")
        );
        channel = chat.CreatePublicConversation("event_tests_channel");
        user = chat.CreateUser("event_tests_user");
        channel.Join();
    }
    
    [Test]
    public void TestModerationEvents()
    {
        var manualModerationEvent = new ManualResetEvent(false);
        chat.OnModerationEvent += moderationEvent =>
        {
            Assert.True(moderationEvent.Payload.Contains("some_reason"));
            manualModerationEvent.Set();
        };
        chat.StartListeningForModerationEvents(user.Id);
        user.SetRestriction(channel.Id, new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "some_reason"
        });
        var moderationEventReceived = manualModerationEvent.WaitOne(5000);
        Assert.IsTrue(moderationEventReceived);
    }
}