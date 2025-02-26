using System.Diagnostics;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

[TestFixture]
public class ChannelTests
{
    private Chat chat;
    private User user;
    private User talkUser;

    [SetUp]
    public async Task Setup()
    {
        chat = await Chat.CreateInstance(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "ctuuuuuuuuuuuuuuuuuuuuuuuuuuuuu")
        );
        if (!chat.TryGetCurrentUser(out user))
        {
            Assert.Fail();
        }
        await user.Update(new ChatUserData()
        {
            Username = "Testificate"
        });
        talkUser = await chat.GetOrCreateUser("talk_user");
    }
    
    [TearDown]
    public async Task CleanUp()
    {
        chat.Destroy();
        await Task.Delay(3000);
    }
    
    [Test]
    public async Task TestGetUserSuggestions()
    {
        var channel = await chat.CreatePublicConversation("user_suggestions_test_channel");
        channel.Join();

        await Task.Delay(5000);
        
        var suggestions = await channel.GetUserSuggestions("@Test");
        Assert.True(suggestions.Any(x => x.UserId == user.Id));
    }
    
    [Test]
    public async Task TestGetMemberships()
    {
        var channel = await chat.CreatePublicConversation("get_members_test_channel");
        channel.Join();
        await Task.Delay(2500);
        var memberships = await channel.GetMemberships();
        Assert.That(memberships.Memberships.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task TestStartTyping()
    {
        var channel = (await chat.CreateDirectConversation(talkUser, "sttc")).CreatedChannel;
        channel.Join();
        await Task.Delay(5500);
        
        var typingManualEvent = new ManualResetEvent(false);

        channel.SetListeningForTyping(true);
        await Task.Delay(5500);
        channel.OnUsersTyping += typingUsers =>
        {
            Assert.That(typingUsers, Does.Contain(user.Id));
            typingManualEvent.Set();
        };
        await channel.StartTyping();
        
        var receivedTyping = typingManualEvent.WaitOne(20000);
        Assert.IsTrue(receivedTyping);
    }
    
    [Test]
    public async Task TestStopTyping()
    {
        var channel = (await chat.CreateDirectConversation(talkUser, "stop_typing_test_channel")).CreatedChannel;
        channel.Join();
        await Task.Delay(2500);
        
        await channel.StartTyping();
        
        await Task.Delay(2500);
        
        var typingManualEvent = new ManualResetEvent(false);
        channel.OnUsersTyping += typingUsers =>
        {
            Assert.That(typingUsers, Is.Empty);
            typingManualEvent.Set();
        };
        await channel.StopTyping();

        var typingEvent = typingManualEvent.WaitOne(6000);
        Assert.IsTrue(typingEvent);
    }
    
    [Test]
    public async Task TestStopTypingFromTimer()
    {
        var channel = (await chat.CreateDirectConversation(talkUser, "stop_typing_timeout_test_channel")).CreatedChannel;
        channel.Join();
        await Task.Delay(2500);
        
        await channel.StartTyping();

        await Task.Delay(3000);
        
        var typingManualEvent = new ManualResetEvent(false);
        channel.OnUsersTyping += typingUsers =>
        {
            Assert.That(typingUsers, Is.Empty);
            typingManualEvent.Set();
        };

        var stoppedTyping = typingManualEvent.WaitOne(10000);
        Assert.IsTrue(stoppedTyping);
    }

    [Test]
    public async Task TestPinMessage()
    {
        var channel = await chat.CreatePublicConversation("pin_message_test_channel_37");
        channel.Join();
        await Task.Delay(2500);
        
        var receivedManualEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            message.SetListeningForUpdates(true);

            await Task.Delay(4000);
            
            await channel.PinMessage(message);
            
            await Task.Delay(2000);

            Assert.True(channel.TryGetPinnedMessage(out var pinnedMessage) && pinnedMessage.MessageText == "message to pin");
            receivedManualEvent.Set();
        };
        await channel.SendText("message to pin");

        var received = receivedManualEvent.WaitOne(19000);
        Assert.IsTrue(received);
    }
    
    [Test]
    public async Task TestUnPinMessage()
    {
        var channel = await chat.CreatePublicConversation("unpin_message_test_channel");
        channel.Join();
        await Task.Delay(2500);
        var receivedManualEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            await channel.PinMessage(message);

            await Task.Delay(2000);
            
            Assert.True(channel.TryGetPinnedMessage(out var pinnedMessage) && pinnedMessage.MessageText == "message to pin");
            await channel.UnpinMessage();
            
            await Task.Delay(2000);
            
            Assert.False(channel.TryGetPinnedMessage(out _));
            receivedManualEvent.Set();
        };
        await channel.SendText("message to pin");

        var received = receivedManualEvent.WaitOne(12000);
        Assert.IsTrue(received);
    }
    
    [Test]
    public async Task TestCreateMessageDraft()
    {
        var channel = await chat.CreatePublicConversation("message_draft_test_channel");
        try
        {
            var draft = channel.CreateMessageDraft();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            Assert.Fail();
        }
    }

    [Test]
    public async Task TestEmitUserMention()
    {
        var channel = await chat.CreatePublicConversation("user_mention_test_channel");
        channel.Join();
        await Task.Delay(2500);
        var receivedManualEvent = new ManualResetEvent(false);
        user.SetListeningForMentionEvents(true);
        user.OnMentionEvent += mentionEvent =>
        {
            Assert.True(mentionEvent.Payload.Contains("heyyy"));
            receivedManualEvent.Set();
        };
        await channel.EmitUserMention(user.Id, "99999999999999999", "heyyy");
        var received = receivedManualEvent.WaitOne(7000);
        Assert.True(received);
    }
}