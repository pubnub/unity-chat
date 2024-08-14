using System.Diagnostics;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

public class MembershipTests
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
            "membership_tests_user_54")
        );
        channel = chat.CreatePublicConversation("membership_tests_channel");
        user = chat.CreateUser("membership_tests_user_54");
        channel.Join();
    }

    [Test]
    public async Task TestGetMemberships()
    {
        var memberships = user.GetMemberships();
        Assert.True(memberships.Memberships.Any(x => x.ChannelId == channel.Id && x.UserId == user.Id));
    }

    [Test]
    public async Task TestUpdateMemberships()
    {
        var memberships = user.GetMemberships();
        var testMembership = memberships.Memberships.Last();
        if (testMembership == null)
        {
            Assert.Fail();
            return;
        }
        var manualUpdatedEvent = new ManualResetEvent(false);
        testMembership.OnMembershipUpdated += membership =>
        {
            Assert.True(membership.Id == testMembership.Id);
            manualUpdatedEvent.Set();
        };
        testMembership.StartListeningForUpdates();

        await Task.Delay(4000);
        
        testMembership.Update("{\"key\": \"" + Guid.NewGuid() + "\"}");
        var updated = manualUpdatedEvent.WaitOne(8000);
        Assert.IsTrue(updated);
    }

    [Test]
    public void TestInvite()
    {
        var testChannel = chat.CreateGroupConversation([user],"test_invite_group_channel").CreatedChannel;
        var testUser = chat.CreateUser("test_invite_user");
        var returnedMembership = testChannel.Invite(testUser);
        Assert.True(returnedMembership.ChannelId == testChannel.Id && returnedMembership.UserId == testUser.Id);
    }

    [Test]
    public void TestInviteMultiple()
    {
        var testChannel = chat.CreateGroupConversation([user],"invite_multiple_test_group_channel_3").CreatedChannel;
        var secondUser = chat.CreateUser("second_invite_user");
        var thirdUser = chat.CreateUser("third_invite_user");
        var returnedMemberships = testChannel.InviteMultiple([
            secondUser,
            thirdUser
        ]);
        Assert.True(
            returnedMemberships.Count == 2 &&
            returnedMemberships.Any(x => x.UserId == secondUser.Id && x.ChannelId == testChannel.Id) &&
            returnedMemberships.Any(x => x.UserId == thirdUser.Id && x.ChannelId == testChannel.Id));
    }

    [Test]
    public async Task TestLastRead()
    {
        var testChannel = chat.CreatePublicConversation("last_read_test_channel_57");
        testChannel.Join();
        
        await Task.Delay(4000);
        
        var membership = user.GetMemberships(limit:20).Memberships
            .FirstOrDefault(x => x.ChannelId == testChannel.Id);
        if (membership == null)
        {
            Assert.Fail();
            return;
        }

        var messageReceivedManual = new ManualResetEvent(false);
        
        testChannel.OnMessageReceived += async message =>
        {
            membership.SetLastReadMessage(message);
            
            await Task.Delay(7000);

            var lastTimeToken = membership.GetLastReadMessageTimeToken();
            Assert.True(lastTimeToken == message.TimeToken);
            membership.SetLastReadMessageTimeToken("99999999999999999");

            await Task.Delay(3000);

            Assert.True(membership.GetLastReadMessageTimeToken() == "99999999999999999");
            messageReceivedManual.Set();
        };
        testChannel.SendText("some_message");

        var received = messageReceivedManual.WaitOne(90000);
        Assert.True(received);
    }

    [Test]
    public async Task TestUnreadMessagesCount()
    {
        var unreadChannel = chat.CreatePublicConversation($"test_channel_{Guid.NewGuid()}");
        unreadChannel.Join();
        unreadChannel.SendText("one");
        unreadChannel.SendText("two");
        unreadChannel.SendText("three");

        await Task.Delay(4000);

        var membership = chat.GetUserMemberships(user.Id, limit:20).Memberships
            .FirstOrDefault(x => x.ChannelId == unreadChannel.Id);
        Assert.True(membership != null && membership.GetUnreadMessagesCount() == 3);
    }
}