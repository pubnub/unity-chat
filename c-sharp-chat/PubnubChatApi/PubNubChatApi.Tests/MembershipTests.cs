using System.Diagnostics;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

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
        chat = await Chat.CreateInstance(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "membership_tests_user_54")
        );
        channel = await chat.OLD_CreatePublicConversation("membership_tests_channel");
        if (!chat.OLD_TryGetCurrentUser(out user))
        {
            Assert.Fail();
        }

        channel.OLD_Join();
        await Task.Delay(3500);
    }

    [TearDown]
    public async Task CleanUp()
    {
        channel.OLD_Leave();
        await Task.Delay(3000);
        chat.Destroy();
        await Task.Delay(3000);
    }

    [Test]
    public async Task TestGetMemberships()
    {
        var memberships = await user.GetMemberships();
        Assert.True(memberships.Memberships.Any(x => x.OLD_ChannelId == channel.Id && x.OLD_UserId == user.Id));
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
            OLD_CustomDataJson = "{\"key\":\"" + Guid.NewGuid() + "\"}"
        };

        var manualUpdatedEvent = new ManualResetEvent(false);
        testMembership.OnMembershipUpdated += membership =>
        {
            Assert.True(membership.Id == testMembership.Id);
            var updatedData = membership.OLD_MembershipData.OLD_CustomDataJson;
            Assert.True(updatedData == updateData.OLD_CustomDataJson, $"{updatedData} != {updateData.OLD_CustomDataJson}");
            manualUpdatedEvent.Set();
        };
        testMembership.SetListeningForUpdates(true);

        await Task.Delay(4000);

        await testMembership.OLD_Update(updateData);
        var updated = manualUpdatedEvent.WaitOne(8000);
        Assert.IsTrue(updated);
    }

    [Test]
    public async Task TestInvite()
    {
        var testChannel = (await chat.OLD_CreateGroupConversation([user], "test_invite_group_channel")).CreatedChannel;
        var testUser = await chat.GetOrCreateUser("test_invite_user");
        var returnedMembership = await testChannel.OLD_Invite(testUser);
        Assert.True(returnedMembership.OLD_ChannelId == testChannel.Id && returnedMembership.OLD_UserId == testUser.Id);
    }

    [Test]
    public async Task TestInviteMultiple()
    {
        var testChannel = (await chat.OLD_CreateGroupConversation([user], "invite_multiple_test_group_channel_3"))
            .CreatedChannel;
        var secondUser = await chat.GetOrCreateUser("second_invite_user");
        var thirdUser = await chat.GetOrCreateUser("third_invite_user");
        var returnedMemberships = await testChannel.OLD_InviteMultiple([
            secondUser,
            thirdUser
        ]);
        Assert.True(
            returnedMemberships.Count == 2 &&
            returnedMemberships.Any(x => x.OLD_UserId == secondUser.Id && x.OLD_ChannelId == testChannel.Id) &&
            returnedMemberships.Any(x => x.OLD_UserId == thirdUser.Id && x.OLD_ChannelId == testChannel.Id));
    }

    [Test]
    public async Task TestLastRead()
    {
        var testChannel = await chat.OLD_CreatePublicConversation("last_read_test_channel_57");
        testChannel.OLD_Join();

        await Task.Delay(4000);

        var membership = (await user.GetMemberships(limit: 20)).Memberships
            .FirstOrDefault(x => x.OLD_ChannelId == testChannel.Id);
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

            var lastTimeToken = membership.OLD_GetLastReadMessageTimeToken();
            Assert.True(lastTimeToken == message.OLD_TimeToken);
            await membership.OLD_SetLastReadMessageTimeToken("99999999999999999");

            await Task.Delay(3000);

            Assert.True(membership.OLD_GetLastReadMessageTimeToken() == "99999999999999999");
            messageReceivedManual.Set();
        };
        await testChannel.SendText("some_message");

        var received = messageReceivedManual.WaitOne(90000);
        Assert.True(received);
    }

    [Test]
    public async Task TestUnreadMessagesCount()
    {
        var unreadChannel = await chat.OLD_CreatePublicConversation($"test_channel_{Guid.NewGuid()}");
        unreadChannel.OLD_Join();
        
        await Task.Delay(3500);
        
        await unreadChannel.SendText("one");
        await unreadChannel.SendText("two");
        await unreadChannel.SendText("three");

        await Task.Delay(8000);
        
        var membership = (await unreadChannel.OLD_GetMemberships())
            .Memberships.FirstOrDefault(x => x.OLD_UserId == user.Id);
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
            .Memberships.FirstOrDefault(x => x.OLD_ChannelId == channel.Id);
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