using System.Diagnostics;
using PubnubApi;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using Channel = PubNubChatAPI.Entities.Channel;

namespace PubNubChatApi.Tests;

[TestFixture]
public class MembershipTests
{
    private Chat chat;
    private Channel channel;
    private User user;

    [SetUp]
    public async Task Setup()
    {
        chat = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(storeUserActivityTimestamp: true), new PNConfiguration(new UserId("membership_tests_user_54"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        }));
        channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("membership_tests_channel"));
        user = TestUtils.AssertOperation(await chat.GetCurrentUser());
        channel.Join();
        await Task.Delay(3500);
    }

    [TearDown]
    public async Task CleanUp()
    {
        await chat.PubnubInstance.RemoveMemberships().Channels(new List<string>() { "membership_tests_channel", "test_invite_group_channel" })
            .Uuid("membership_tests_user_54").ExecuteAsync();
        channel.Leave();
        await Task.Delay(3000);
        chat.Destroy();
        await Task.Delay(3000);
    }

    [Test]
    public async Task TestGetMemberships()
    {
        var memberships = await user.GetMemberships();
        Assert.True(memberships.Memberships.Any(x => x.ChannelId == channel.Id && x.UserId == user.Id));
    }

    [Test]
    public async Task TestUpdateMemberships()
    {
        var memberships = await user.GetMemberships();
        var testMembership = memberships.Memberships.Last();
        if (testMembership == null)
        {
            Assert.Fail();
            return;
        }

        var updateData = new ChatMembershipData()
        {
            CustomData = new Dictionary<string, object>()
            {
                {"key", Guid.NewGuid().ToString()}
            },
            Type = "some_membership",
            Status = "active"
        };

        var manualUpdatedEvent = new ManualResetEvent(false);
        testMembership.OnMembershipUpdated += membership =>
        {
            Assert.True(membership.Id == testMembership.Id);
            var updatedData = membership.MembershipData.CustomData;
            Assert.True(updatedData["key"].ToString() == updateData.CustomData["key"].ToString());
            manualUpdatedEvent.Set();
        };
        testMembership.SetListeningForUpdates(true);

        await Task.Delay(4000);

        await testMembership.Update(updateData);
        var updated = manualUpdatedEvent.WaitOne(8000);
        Assert.IsTrue(updated);
    }

    [Test]
    public async Task TestInvite()
    {
        var testChannel = TestUtils.AssertOperation(await chat.CreateGroupConversation([user], "test_invite_group_channel")).CreatedChannel;
        var testUser = await chat.GetOrCreateUser("test_invite_user");
        var returnedMembership = TestUtils.AssertOperation(await testChannel.Invite(testUser));
        Assert.True(returnedMembership.ChannelId == testChannel.Id && returnedMembership.UserId == testUser.Id);
    }

    [Test]
    public async Task TestInviteMultiple()
    {
        var testChannel = TestUtils.AssertOperation(await chat.CreateGroupConversation([user], "invite_multiple_test_group_channel_3"))
            .CreatedChannel;
        var secondUser = await chat.GetOrCreateUser("second_invite_user");
        var thirdUser = await chat.GetOrCreateUser("third_invite_user");
        var returnedMemberships = TestUtils.AssertOperation(await testChannel.InviteMultiple([
            secondUser,
            thirdUser
        ]));
        Assert.True(
            returnedMemberships.Count == 2 &&
            returnedMemberships.Any(x => x.UserId == secondUser.Id && x.ChannelId == testChannel.Id) &&
            returnedMemberships.Any(x => x.UserId == thirdUser.Id && x.ChannelId == testChannel.Id));
    }

    [Test]
    public async Task TestLastRead()
    {
        var testChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation("last_read_test_channel_57"));
        testChannel.Join();

        await Task.Delay(4000);

        var membership = (await user.GetMemberships(limit: 20)).Memberships
            .FirstOrDefault(x => x.ChannelId == testChannel.Id);
        if (membership == null)
        {
            Assert.Fail();
            return;
        }

        var messageReceivedManual = new ManualResetEvent(false);

        testChannel.OnMessageReceived += async message =>
        {
            await membership.SetLastReadMessage(message);

            await Task.Delay(7000);

            var lastTimeToken = membership.LastReadMessageTimeToken;
            Assert.True(lastTimeToken == message.TimeToken);
            await membership.SetLastReadMessageTimeToken("99999999999999999");

            await Task.Delay(3000);

            Assert.True(membership.LastReadMessageTimeToken == "99999999999999999");
            messageReceivedManual.Set();
        };
        await testChannel.SendText("some_message");

        var received = messageReceivedManual.WaitOne(90000);
        Assert.True(received);
    }

    [Test]
    public async Task TestUnreadMessagesCount()
    {
        var unreadChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation($"test_channel_{Guid.NewGuid()}"));
        unreadChannel.Join();
        
        await Task.Delay(3500);
        
        await unreadChannel.SendText("one");
        await unreadChannel.SendText("two");
        await unreadChannel.SendText("three");

        await Task.Delay(8000);
        
        var membership = TestUtils.AssertOperation(await unreadChannel.GetMemberships())
            .Memberships.FirstOrDefault(x => x.UserId == user.Id);
        var unreadCount = membership == null ? -1 : await membership.GetUnreadMessagesCount();
        Assert.True(unreadCount >= 3, $"Expected >=3 unread but got: {unreadCount}");
    }
    
    [Test]
    //Test added after a specific user issue where calling membership.GetUnreadMessagesCount()
    //after a history fetch would throw a C-Core PNR_RX_BUFF_NOT_EMPTY error 
    public async Task TestUnreadCountAfterFetchHistory()
    {
        await channel.SendText("some_text");
        var membership = (await user.GetMemberships())
            .Memberships.FirstOrDefault(x => x.ChannelId == channel.Id);
        if (membership == null)
        {
            Assert.Fail("Couldn't find membership");
            return;
        }
        await Task.Delay(5000);
        var history = await channel.GetMessageHistory("99999999999999999", "00000000000000000", 1);
        var unread = await membership.GetUnreadMessagesCount();
        Assert.True(unread >= 1);
    }
}