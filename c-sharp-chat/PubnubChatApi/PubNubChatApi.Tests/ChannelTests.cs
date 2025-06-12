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
    public async Task TestUpdateChannel()
    {
        var channel = await chat.CreatePublicConversation();
        channel.SetListeningForUpdates(true);

        await Task.Delay(3000);

        var updateReset = new ManualResetEvent(false);
        var updatedData = new ChatChannelData()
        {
            ChannelDescription = "some description",
            ChannelCustomDataJson = "{\"key\":\"value\"}",
            ChannelName = "some name",
            ChannelStatus = "yes",
            ChannelType = "sometype"
        };
        channel.OnChannelUpdate += updatedChannel =>
        {
            Assert.True(updatedChannel.Description == updatedData.ChannelDescription, "updatedChannel.Description != updatedData.ChannelDescription");
            Assert.True(updatedChannel.CustomDataJson == updatedData.ChannelCustomDataJson, "updatedChannel.CustomDataJson != updatedData.ChannelCustomDataJson");
            Assert.True(updatedChannel.Name == updatedData.ChannelName, "updatedChannel.Name != updatedData.ChannelDescription");
            Assert.True(updatedChannel.Status == updatedData.ChannelStatus, "updatedChannel.Status != updatedData.ChannelStatus");
            Assert.True(updatedChannel.Type == updatedData.ChannelType, "updatedChannel.Type != updatedData.ChannelType");
            updateReset.Set();
        };
        await channel.Update(updatedData);
        var updated = updateReset.WaitOne(15000);
        Assert.True(updated);
    }

    [Test]
    public async Task TestDeleteChannel()
    {
        var channel = await chat.CreatePublicConversation();

        await Task.Delay(3000);
        
        Assert.True(chat.TryGetChannel(channel.Id, out _), "Couldn't fetch created channel from chat");
        
        await channel.Delete();

        await Task.Delay(3000);
        
        Assert.False(chat.TryGetChannel(channel.Id, out _), "Fetched the supposedly-deleted channel from chat");
    }
    
    [Test]
    public async Task TestLeaveChannel()
    {
        var currentChatUser = await chat.GetCurrentUserAsync();
        
        Assert.IsNotNull(currentChatUser, "currentChatUser was null");
        
        var channel = await chat.CreatePublicConversation();
        channel.Join();

        await Task.Delay(3000);

        var memberships = await channel.GetMemberships();
        
        Assert.True(memberships.Memberships.Any(x => x.UserId == currentChatUser.Id), "Join failed, current user not found in channel memberships");
        
        channel.Leave();
        
        await Task.Delay(3000);
        
        memberships = await channel.GetMemberships();
        
        Assert.False(memberships.Memberships.Any(x => x.UserId == currentChatUser.Id), "Leave failed, current user found in channel memberships");
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
        await Task.Delay(3500);
        var memberships = await channel.GetMemberships();
        Assert.That(memberships.Memberships.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task TestStartTyping()
    {
        var channel = (await chat.CreateDirectConversation(talkUser, "sttc")).CreatedChannel;
        channel.Join();
        await Task.Delay(2500);
        channel.SetListeningForTyping(true);
        
        await Task.Delay(5500);
        
        var typingManualEvent = new ManualResetEvent(false);
        channel.OnUsersTyping += typingUsers =>
        {
            Assert.That(typingUsers, Does.Contain(user.Id));
            typingManualEvent.Set();
        };
        await channel.StartTyping();
        
        var receivedTyping = typingManualEvent.WaitOne(12000);
        Assert.IsTrue(receivedTyping);
    }
    
    [Test]
    public async Task TestStopTyping()
    {
        var channel = (await chat.CreateDirectConversation(talkUser, "stop_typing_test_channel")).CreatedChannel;
        channel.Join();
        await Task.Delay(2500);
        channel.SetListeningForTyping(true);
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
        channel.SetListeningForTyping(true);
        
        await Task.Delay(4500);
        
        await channel.StartTyping();

        await Task.Delay(3000);
        
        var typingManualEvent = new ManualResetEvent(false);
        channel.OnUsersTyping += typingUsers =>
        {
            Assert.That(typingUsers, Is.Empty);
            typingManualEvent.Set();
        };

        var stoppedTyping = typingManualEvent.WaitOne(12000);
        Assert.IsTrue(stoppedTyping);
    }

    [Test]
    public async Task TestPinMessage()
    {
        var channel = await chat.CreatePublicConversation("pin_message_test_channel_37");
        channel.Join();
        await Task.Delay(3500);
        
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
        await Task.Delay(3500);
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
        await Task.Delay(3000);
        user.OnMentionEvent += mentionEvent =>
        {
            Assert.True(mentionEvent.Payload.Contains("heyyy"));
            receivedManualEvent.Set();
        };
        await channel.EmitUserMention(user.Id, "99999999999999999", "heyyy");
        var received = receivedManualEvent.WaitOne(7000);
        Assert.True(received);
    }
    
    [Test]
    public async Task TestChannelIsPresent()
    {
        var someChannel = await chat.CreatePublicConversation();
        someChannel.Join();

        await Task.Delay(4000);

        var isPresent = await someChannel.IsUserPresent(user.Id);
        
        Assert.True(isPresent, "someChannel.IsUserPresent() doesn't return true for most recently joined channel!");
    }
    
    [Test]
    public async Task TestChannelWhoIsPresent()
    {
        var someChannel = await chat.CreatePublicConversation();
        someChannel.Join();

        await Task.Delay(4000);

        var who = await someChannel.WhoIsPresent();
        
        Assert.Contains(user.Id, who, "channel.WhoIsPresent() doesn't have most recently joine user!");
    }
}