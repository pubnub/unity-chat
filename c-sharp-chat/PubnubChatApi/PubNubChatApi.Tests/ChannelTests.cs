using System.Diagnostics;
using PubnubApi;
using PubnubChatApi;

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
        channel.OnUpdated += updatedChannel =>
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
    public async Task TestHardDeleteChannel()
    {
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation());

        await Task.Delay(3000);
        
        var channelExists = await chat.GetChannel(channel.Id);
        Assert.False(channelExists.Error, "Couldn't fetch created channel from chat");

        await channel.Delete(false);

        await Task.Delay(3000);
        
        var channelAfterDelete = await chat.GetChannel(channel.Id);
        Assert.True(channelAfterDelete.Error, "Fetched the supposedly-deleted channel from chat");
    }
    
    [Test]
    public async Task TestSoftDeleteAndRestoreChannel()
    {
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation());

        await Task.Delay(3000);
        
        var channelExists = await chat.GetChannel(channel.Id);
        Assert.False(channelExists.Error, "Couldn't fetch created channel from chat");

        await channel.Delete(true);

        await Task.Delay(3000);
        
        var channelAfterDelete = await chat.GetChannel(channel.Id);
        Assert.False(channelAfterDelete.Error, "Channel should still exist after soft-delete");
        Assert.True(channelAfterDelete.Result.IsDeleted, "Channel should be marked as soft-deleted");

        await channelAfterDelete.Result.Restore();
        Assert.False(channelAfterDelete.Result.IsDeleted, "Channel should be restored");
        
        await Task.Delay(3000);
        
        var channelAfterRestore = await chat.GetChannel(channel.Id);
        Assert.False(channelAfterRestore.Error, "Channel should still exist after restore");
        Assert.False(channelAfterRestore.Result.IsDeleted, "Channel fetched from server again should be marked as not-deleted after restore");
    }
    
    [Test]
    public async Task TestLeaveChannel()
    {
        var currentChatUser = TestUtils.AssertOperation(await chat.GetCurrentUser());
        
        Assert.IsNotNull(currentChatUser, "currentChatUser was null");
        
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        await channel.Join();

        await Task.Delay(3000);

        var memberships = TestUtils.AssertOperation(await channel.GetMemberships());
        
        Assert.True(memberships.Memberships.Any(x => x.UserId == currentChatUser.Id), "Join failed, current user not found in channel memberships");
        
        channel.Leave();
        
        await Task.Delay(3000);
        
        memberships = TestUtils.AssertOperation(await channel.GetMemberships());
        
        Assert.False(memberships.Memberships.Any(x => x.UserId == currentChatUser.Id), "Leave failed, current user found in channel memberships");
    }
    
    [Test]
    public async Task TestGetMessagesHistory()
    {
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        channel.OnMessageReceived += async message =>
        {
            TestUtils.AssertOperation(await message.EditMessageText("some_new_text"));
        };
        await channel.Join();
        await Task.Delay(3500);
        TestUtils.AssertOperation(await channel.SendText("wololo"));
        
        await Task.Delay(10000);

        var history =
            TestUtils.AssertOperation(await channel.GetMessageHistory("99999999999999999", "00000000000000000", 1));
        
        Assert.True(history != null, "history was null null");
        Assert.True(history.Count == 1, "history count was wrong");
        Assert.True(history[0].OriginalMessageText == "wololo", "message from history had wrong original text");
        Assert.True(history[0].MessageText == "some_new_text", "message from history had wrong text");
    }
    
    [Test]
    public async Task TestGetMemberships()
    {
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("get_members_test_channel"));
        await channel.Join();
        await Task.Delay(3500);
        var memberships = TestUtils.AssertOperation(await channel.GetMemberships());
        Assert.That(memberships.Memberships.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task TestGetInvitees()
    {
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        await channel.Invite(user);
        await Task.Delay(3500);
        var invitees = TestUtils.AssertOperation(await channel.GetInvitees());
        Assert.True(invitees.Memberships.Any(x => x.UserId == user.Id && x.ChannelId == channel.Id && x.MembershipData.Status == "pending"));
        
        //Cleanup
        await channel.Delete();
    }

    [Test]
    public async Task TestInviteAndJoin()
    {
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        await channel.Invite(user);
        await Task.Delay(3500);
        var invitees = TestUtils.AssertOperation(await channel.GetInvitees());
        Assert.True(invitees.Memberships.Any(x => x.UserId == user.Id && x.ChannelId == channel.Id && x.MembershipData.Status == "pending"));
        await channel.Join();
        await Task.Delay(3500);
        invitees = TestUtils.AssertOperation(await channel.GetInvitees());
        Assert.False(invitees.Memberships.Any());
        var members = TestUtils.AssertOperation(await channel.GetMemberships());
        Assert.True(members.Memberships.Any(x => x.UserId == user.Id && x.ChannelId == channel.Id && x.MembershipData.Status != "pending"));
    }

    [Test]
    public async Task TestStartTyping()
    {
        var channel = TestUtils.AssertOperation(await chat.CreateDirectConversation(talkUser, "sttc")).CreatedChannel;
        await channel.Join();
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
        await channel.Join();
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
        await channel.Join();
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
        await channel.Join();
        await Task.Delay(3500);
        
        var receivedManualEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            message.SetListeningForUpdates(true);

            await Task.Delay(4000);
            
            TestUtils.AssertOperation(await channel.PinMessage(message));
            
            await Task.Delay(2000);

            var pinned = TestUtils.AssertOperation(await channel.GetPinnedMessage());
            Assert.True(pinned.MessageText == "message to pin");
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
        await channel.Join();
        await Task.Delay(3500);
        var receivedManualEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            TestUtils.AssertOperation(await channel.PinMessage(message));

            await Task.Delay(2000);

            var pinned = TestUtils.AssertOperation(await channel.GetPinnedMessage());
            Assert.True(pinned.MessageText == "message to pin");
            TestUtils.AssertOperation(await channel.UnpinMessage());
            
            await Task.Delay(15000);

            var getPinned = await channel.GetPinnedMessage();
            Assert.True(getPinned.Error);
            receivedManualEvent.Set();
        };
        await channel.SendText("message to pin");

        var received = receivedManualEvent.WaitOne(35000);
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
        await channel.Join();
        await Task.Delay(2500);
        var receivedManualEvent = new ManualResetEvent(false);
        user.SetListeningForMentionEvents(true);
        await Task.Delay(3000);
        user.OnMentioned += mentionEvent =>
        {
            Assert.True(mentionEvent.Text.Contains("heyyy"));
            Assert.True(mentionEvent.MentionedByUserId == user.Id);
            Assert.True(mentionEvent.ChannelId == channel.Id);
            receivedManualEvent.Set();
        };
        var draft = channel.CreateMessageDraft();
        draft.InsertText(0, "heyyy");
        draft.AddMention(0, 5, new MentionTarget()
        {
            Type = MentionType.User,
            Target = user.Id
        });
        await draft.Send();
        var received = receivedManualEvent.WaitOne(7000);
        Assert.True(received);
    }
    
    [Test]
    public async Task TestEmitCustomEvent()
    {
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("channel_custom_event_test"));
        await channel.Join();
        await Task.Delay(2500);
        var reportManualEvent = new ManualResetEvent(false);
        channel.OnCustomEvent += customEvent =>
        {
            Assert.True(customEvent.Payload.Contains("test"));
            Assert.True(customEvent.Payload.Contains("some_nonsense"));
            reportManualEvent.Set();
        };
        channel.SetListeningForCustomEvents(true);
        await Task.Delay(2500);
        await channel.EmitCustomEvent("{\"test\":\"some_nonsense\"}");

        var eventReceived = reportManualEvent.WaitOne(8000);
        Assert.True(eventReceived);
    }
    
    [Test]
    public async Task TestChannelIsPresent()
    {
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        await someChannel.Join();

        await Task.Delay(4000);

        var isPresent = TestUtils.AssertOperation(await someChannel.IsUserPresent(user.Id));
        
        Assert.True(isPresent, "someChannel.IsUserPresent() doesn't return true for most recently joined channel!");
    }
    
    [Test]
    public async Task TestChannelHasAndGetMember()
    {
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        await someChannel.Join();

        await Task.Delay(4000);

        var hasMember = TestUtils.AssertOperation(await someChannel.HasMember(user.Id));
        Assert.True(hasMember, "someChannel.HasMember() doesn't return true for most recently joined channel!");

        var getMember = TestUtils.AssertOperation(await someChannel.GetMember(user.Id));
        Assert.True(getMember.ChannelId == someChannel.Id, "Wrong GetMember() channel id");
        Assert.True(getMember.UserId == user.Id, "Wrong GetMember() user id");
        
    }
    
    [Test]
    public async Task TestChannelWhoIsPresent()
    {
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        await someChannel.Join();

        await Task.Delay(4000);

        var who = TestUtils.AssertOperation(await someChannel.WhoIsPresent());
        
        Assert.Contains(user.Id, who, "channel.WhoIsPresent() doesn't have most recently joine user!");
    }
    
    [Test]
    public async Task TestPresenceCallback()
    {
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        someChannel.StreamPresence(true);

        var reset = new ManualResetEvent(false);
        someChannel.OnPresenceUpdate += userIds =>
        {
            Assert.True(userIds.Contains(user.Id), "presence callback doesn't contain joined user id");
            reset.Set();
        };
        await someChannel.Join();
        var presenceReceived = reset.WaitOne(12000);
        Assert.True(presenceReceived, "did not receive presence callback");
        
        //Cleanup
        someChannel.StreamPresence(false);
        await someChannel.Delete();
    }

    [Test]
    public async Task TestDeletionCallback()
    {
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        someChannel.StreamUpdates(true);

        await Task.Delay(2500);

        var deleteReset = new ManualResetEvent(false);
        someChannel.OnDeleted += () =>
        {
            deleteReset.Set();
        };
        TestUtils.AssertOperation(await someChannel.Delete());
        var deleted = deleteReset.WaitOne(15000);
        Assert.True(deleted, "Didn't receive OnDeleted callback!");
    }

    [Test]
    public async Task TestFetchReadReceipts()
    {
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        await someChannel.Join();
        await Task.Delay(2500);

        var reset = new ManualResetEvent(false);
        Message readMessage = null;
        var messageValue = "READ MEEEE";
        someChannel.OnMessageReceived += message =>
        {
            if (message.MessageText == messageValue)
            {
                readMessage = message;
                reset.Set();
            }
        };
        await someChannel.SendText(messageValue);
        
        var gotMessage = reset.WaitOne(20000);
        Assert.True(gotMessage, "Never received message callback.");

        var membership = TestUtils.AssertOperation(await user.GetMembership(someChannel.Id));
        TestUtils.AssertOperation(await membership.SetLastReadMessage(readMessage));
        await Task.Delay(8000);

        var receipts = TestUtils.AssertOperation(await someChannel.GetReadReceipts());
        
        Assert.True(receipts.Any(x => x.LastReadTimeToken == readMessage.TimeToken && x.UserId == user.Id));
    }
    
    [Test]
    public async Task TestReportCallback()
    {
        var someChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        someChannel.SetListeningForReportEvents(true);
        var reset = new ManualResetEvent(false);
        someChannel.OnMessageReported += reportEvent =>
        {
            Assert.True(reportEvent.Reason == "some_reason", "incorrect report reason received");
            reset.Set();
        };
        
        await someChannel.Join();
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