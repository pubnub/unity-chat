using System.Diagnostics;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

public class ChannelTests
{
    private Chat chat;
    private User user;
    private User talkUser;

    [SetUp]
    public void Setup()
    {
        chat = new Chat(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "channel_tests_user")
        );
        user = chat.CreateUser("channel_tests_user", new ChatUserData()
        {
            Username = "the_channel_tests_user"
        });
        talkUser = chat.CreateUser("talk_user");
    }
    
    [Test]
    public async Task TestGetUserSuggestions()
    {
        var channel = chat.CreatePublicConversation("user_suggestions_test_channel");
        channel.Join();

        await Task.Delay(5000);
        
        var suggestions = channel.GetUserSuggestions("@the");
        Assert.True(suggestions.Any(x => x.UserId == user.Id));
    }
    
    [Test]
    public void TestGetMemberships()
    {
        var channel = chat.CreatePublicConversation("get_members_test_channel");
        channel.Join();
        var memberships =  channel.GetMemberships();
        Assert.That(memberships.Memberships.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void TestStartTyping()
    {
        var channel = chat.CreateDirectConversation(talkUser, "start_typing_test_channel").CreatedChannel;
        channel.Join();
        
        var typingManualEvent = new ManualResetEvent(false);
        
        channel.OnUsersTyping += typingUsers =>
        {
            Assert.That(typingUsers, Does.Contain(user.Id));
            typingManualEvent.Set();
        };
        channel.StartTyping();
        
        var receivedTyping = typingManualEvent.WaitOne(5000);
        Assert.IsTrue(receivedTyping);
    }
    
    [Test]
    public async Task TestStopTyping()
    {
        var channel = chat.CreateDirectConversation(talkUser, "stop_typing_test_channel").CreatedChannel;
        channel.Join();
        
        channel.StartTyping();
        
        await Task.Delay(2500);
        
        var typingManualEvent = new ManualResetEvent(false);
        channel.OnUsersTyping += typingUsers =>
        {
            Assert.That(typingUsers, Is.Empty);
            typingManualEvent.Set();
        };
        channel.StopTyping();

        var typingEvent = typingManualEvent.WaitOne(6000);
        Assert.IsTrue(typingEvent);
    }
    
    [Test]
    public async Task TestStopTypingFromTimer()
    {
        var channel = chat.CreateDirectConversation(talkUser, "stop_typing_timeout_test_channel").CreatedChannel;
        channel.Join();
        
        channel.StartTyping();

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
    public void TestPinMessage()
    {
        var channel = chat.CreatePublicConversation("pin_message_test_channel_37");
        channel.Join();
        
        var receivedManualEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            channel.PinMessage(message);
            
            await Task.Delay(2000);

            Assert.True(channel.TryGetPinnedMessage(out var pinnedMessage) && pinnedMessage.MessageText == "message to pin");
            receivedManualEvent.Set();
        };
        channel.SendText("message to pin");

        var received = receivedManualEvent.WaitOne(9000);
        Assert.IsTrue(received);
    }
    
    [Test]
    public void TestUnPinMessage()
    {
        var channel = chat.CreatePublicConversation("unpin_message_test_channel");
        channel.Join();
        var receivedManualEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            channel.PinMessage(message);

            await Task.Delay(2000);
            
            Assert.True(channel.TryGetPinnedMessage(out var pinnedMessage) && pinnedMessage.MessageText == "message to pin");
            channel.UnpinMessage();
            
            await Task.Delay(2000);
            
            Assert.False(channel.TryGetPinnedMessage(out _));
            receivedManualEvent.Set();
        };
        channel.SendText("message to pin");

        var received = receivedManualEvent.WaitOne(6000);
        Assert.IsTrue(received);
    }
    
    /*[Test]
    public void TestCreateMessageDraft()
    {
        var channel = chat.CreatePublicConversation("message_draft_test_channel");
        try
        {
            var draft = channel.CreateMessageDraft();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            Assert.Fail();
        }
    }*/

    [Test]
    public void TestEmitUserMention()
    {
        var channel = chat.CreatePublicConversation("user_mention_test_channel");
        channel.Join();
        var receivedManualEvent = new ManualResetEvent(false);
        chat.StartListeningForMentionEvents(user.Id);
        chat.OnMentionEvent += mentionEvent =>
        {
            Assert.True(mentionEvent.Payload.Contains("heyyy"));
            receivedManualEvent.Set();
        };
        channel.EmitUserMention(user.Id, "99999999999999999", "heyyy");
        var received = receivedManualEvent.WaitOne(7000);
        Assert.True(received);
    }
}