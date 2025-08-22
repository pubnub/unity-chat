using System.Diagnostics;
using PubnubApi;
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
        chat = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(storeUserActivityTimestamp: true), new PNConfiguration(new UserId("ctuuuuuuuuuuuuuuuuuuuuuuuuuuuuu"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        }));
        user = TestUtils.AssertOperation(await chat.GetCurrentUser());
        TestUtils.AssertOperation(await user.Update(new ChatUserData()
        {
            Username = "Testificate"
        }));
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
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        channel.SetListeningForUpdates(true);

        await Task.Delay(3000);

        var updateReset = new ManualResetEvent(false);
        var updatedData = new ChatChannelData()
        {
            Description = "some description",
            CustomData = new Dictionary<string, object>(){{"key", "value"}},
            Name = "some name",
            Status = "yes",
            Type = "sometype"
        };
        channel.OnChannelUpdate += updatedChannel =>
        {
            Assert.True(updatedChannel.Description == updatedData.Description, "updatedChannel.Description != updatedData.ChannelDescription");
            Assert.True(updatedChannel.CustomData.TryGetValue("key", out var value) && value.ToString() == "value", "updatedChannel.CustomDataJson != updatedData.ChannelCustomDataJson");
            Assert.True(updatedChannel.Name == updatedData.Name, "updatedChannel.Name != updatedData.ChannelDescription");
            Assert.True(updatedChannel.Status == updatedData.Status, "updatedChannel.Status != updatedData.ChannelStatus");
            Assert.True(updatedChannel.Type == updatedData.Type, "updatedChannel.Type != updatedData.ChannelType");
            updateReset.Set();
        };
        TestUtils.AssertOperation(await channel.Update(updatedData));
        var updated = updateReset.WaitOne(15000);
        Assert.True(updated);
    }

    [Test]
    public async Task TestDeleteChannel()
    {
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation());

        await Task.Delay(3000);
        
        var channelExists = await chat.GetChannel(channel.Id);
        Assert.False(channelExists.Error, "Couldn't fetch created channel from chat");

        await channel.Delete();

        await Task.Delay(3000);
        
        var channelAfterDelete = await chat.GetChannel(channel.Id);
        Assert.True(channelAfterDelete.Error, "Fetched the supposedly-deleted channel from chat");
    }
    
    [Test]
    public async Task TestLeaveChannel()
    {
        var currentChatUser = TestUtils.AssertOperation(await chat.GetCurrentUser());
        
        Assert.IsNotNull(currentChatUser, "currentChatUser was null");
        
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        channel.Join();

        await Task.Delay(3000);

        var memberships = TestUtils.AssertOperation(await channel.GetMemberships());
        
        Assert.True(memberships.Memberships.Any(x => x.UserId == currentChatUser.Id), "Join failed, current user not found in channel memberships");
        
        channel.Leave();
        
        await Task.Delay(3000);
        
        memberships = TestUtils.AssertOperation(await channel.GetMemberships());
        
        Assert.False(memberships.Memberships.Any(x => x.UserId == currentChatUser.Id), "Leave failed, current user found in channel memberships");
    }
    
    [Test]
    public async Task TestGetMemberships()
    {
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("get_members_test_channel"));
        channel.Join();
        await Task.Delay(3500);
        var memberships = TestUtils.AssertOperation(await channel.GetMemberships());
        Assert.That(memberships.Memberships.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task TestStartTyping()
    {
        var channel = TestUtils.AssertOperation(await chat.CreateDirectConversation(talkUser, "sttc")).CreatedChannel;
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
        TestUtils.AssertOperation(await channel.StartTyping());
        
        var receivedTyping = typingManualEvent.WaitOne(12000);
        Assert.IsTrue(receivedTyping);
    }
    
    [Test]
    public async Task TestStopTyping()
    {
        var channel = TestUtils.AssertOperation(await chat.CreateDirectConversation(talkUser, "stop_typing_test_channel")).CreatedChannel;
        channel.Join();
        await Task.Delay(2500);
        channel.SetListeningForTyping(true);
        await Task.Delay(2500);
        
        TestUtils.AssertOperation(await channel.StartTyping());
        
        await Task.Delay(2500);
        
        var typingManualEvent = new ManualResetEvent(false);
        channel.OnUsersTyping += typingUsers =>
        {
            Assert.That(typingUsers, Is.Empty);
            typingManualEvent.Set();
        };
        TestUtils.AssertOperation(await channel.StopTyping());

        var typingEvent = typingManualEvent.WaitOne(6000);
        Assert.IsTrue(typingEvent);
    }
    
    [Test]
    public async Task TestStopTypingFromTimer()
    {
        var channel = TestUtils.AssertOperation(await chat.CreateDirectConversation(talkUser, "stop_typing_timeout_test_channel")).CreatedChannel;
        channel.Join();
        await Task.Delay(2500);
        channel.SetListeningForTyping(true);
        
        await Task.Delay(4500);
        
        TestUtils.AssertOperation(await channel.StartTyping());

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
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("pin_message_test_channel_37"));
        channel.Join();
        await Task.Delay(3500);
        
        var receivedManualEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            message.SetListeningForUpdates(true);

            await Task.Delay(4000);
            
            TestUtils.AssertOperation(await channel.PinMessage(message));
            
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
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("unpin_message_test_channel"));
        channel.Join();
        await Task.Delay(3500);
        var receivedManualEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            TestUtils.AssertOperation(await channel.PinMessage(message));

            await Task.Delay(2000);
            
            Assert.True(channel.TryGetPinnedMessage(out var pinnedMessage) && pinnedMessage.MessageText == "message to pin");
            TestUtils.AssertOperation(await channel.UnpinMessage());
            
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
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("message_draft_test_channel"));
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
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("user_mention_test_channel"));
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
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        someChannel.Join();

        await Task.Delay(4000);

        var isPresent = TestUtils.AssertOperation(await someChannel.IsUserPresent(user.Id));
        
        Assert.True(isPresent, "someChannel.IsUserPresent() doesn't return true for most recently joined channel!");
    }
    
    [Test]
    public async Task TestChannelWhoIsPresent()
    {
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        someChannel.Join();

        await Task.Delay(4000);

        var who = TestUtils.AssertOperation(await someChannel.WhoIsPresent());
        
        Assert.Contains(user.Id, who, "channel.WhoIsPresent() doesn't have most recently joine user!");
    }
    
    [Test]
    public async Task TestPresenceCallback()
    {
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        someChannel.SetListeningForPresence(true);

        var reset = new ManualResetEvent(false);
        someChannel.OnPresenceUpdate += userIds =>
        {
            Assert.True(userIds.Contains(user.Id), "presence callback doesn't contain joined user id");
            reset.Set();
        };
        someChannel.Join();
        var presenceReceived = reset.WaitOne(12000);
        
        Assert.True(presenceReceived, "did not receive presence callback");
    }
    
    [Test]
    public async Task TestReportCallback()
    {
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        someChannel.SetListeningForReportEvents(true);
        var reset = new ManualResetEvent(false);
        someChannel.OnReportEvent += reportEvent =>
        {
            var data = chat.PubnubInstance.JsonPluggableLibrary.DeserializeToDictionaryOfObject(reportEvent.Payload);
            Assert.True(data.TryGetValue("reason", out var reason) && reason.ToString() == "some_reason", "incorrect report reason received");
            reset.Set();
        };
        
        someChannel.Join();
        await Task.Delay(3000);

        someChannel.OnMessageReceived += async message =>
        {
            await message.Report("some_reason");
        };
        await someChannel.SendText("message_to_be_reported");
        
        var reportReceived = reset.WaitOne(12000);
        
        Assert.True(reportReceived, "did not receive report callback");
    }
}