using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Enums;

namespace PubNubChatApi.Tests;

public class ChatTests
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
            "chats_tests_user_10_no_calkiem_nowy"));
        channel = chat.CreatePublicConversation("chat_tests_channel");
        channel.Join();
    }

    [Test]
    public void TestGetCurrentUser()
    {
        Assert.True(chat.TryGetCurrentUser(out var currentUser) && currentUser.Id == user.Id);
    }

    [Test]
    public async Task TestGetUserSuggestions()
    {
        var suggestedUser = chat.CreateUser("some_guy", new ChatUserData()
        {
            Username = "THE_GUY"
        });

        await Task.Delay(5000);

        var suggestions = chat.GetUserSuggestions("@THE");
        Assert.True(suggestions.Any(x => x.Id == suggestedUser.Id));
    }

    [Test]
    public async Task TestGetEventHistory()
    {
        chat.EmitEvent(PubnubChatEventType.Custom, channel.Id, "{\"test\":\"some_nonsense\"}");

        await Task.Delay(5000);

        var history = chat.GetEventsHistory(channel.Id, "99999999999999999", "00000000000000000", 50);
        Assert.True(history.Events.Any(x => x.ChannelId == channel.Id));
    }

    [Test]
    public async Task TestGetChannelSuggestions()
    {
        var suggestedChannel = chat.CreatePublicConversation("suggested_channel", new ChatChannelData()
        {
            ChannelName = "SUGGESTED_CHANNEL_NAME"
        });

        await Task.Delay(5000);

        var suggestions = chat.GetChannelSuggestions("#SUGGESTED");
        Assert.True(suggestions.Any(x => x.Id == suggestedChannel.Id));
    }

    [Test]
    public void TestGetUsers()
    {
        var users = chat.GetUsers();
        Assert.True(users.Users.Any(x => x.Id == user.Id));
    }

    [Test]
    public void TestGetChannels()
    {
        var channels = chat.GetChannels();
        Assert.True(channels.Channels.Any(x => x.Id == channel.Id));
    }

    [Test]
    public void TestCreateDirectConversation()
    {
        var convoUser = chat.CreateUser("direct_conversation_user");
        var directConversation =
            chat.CreateDirectConversation(convoUser, "direct_conversation_test");
        Assert.True(directConversation.CreatedChannel is { Id: "direct_conversation_test" });
        Assert.True(directConversation.HostMembership != null && directConversation.HostMembership.UserId == user.Id);
        Assert.True(directConversation.InviteesMemberships != null &&
                    directConversation.InviteesMemberships.First().UserId == convoUser.Id);
    }

    [Test]
    public void TestCreateGroupConversation()
    {
        var convoUser1 = chat.CreateUser("group_conversation_user_1");
        var convoUser2 = chat.CreateUser("group_conversation_user_2");
        var convoUser3 = chat.CreateUser("group_conversation_user_3");
        var groupConversation =
            chat.CreateGroupConversation([convoUser1, convoUser2, convoUser3], "group_conversation_test");
        Assert.True(groupConversation.CreatedChannel is { Id: "group_conversation_test" });
        Assert.True(groupConversation.HostMembership != null && groupConversation.HostMembership.UserId == user.Id);
        Assert.True(groupConversation.InviteesMemberships is { Count: 3 });
        Assert.True(groupConversation.InviteesMemberships.Any(x =>
            x.UserId == convoUser1.Id && x.ChannelId == "group_conversation_test"));
    }

    [Test]
    public void TestForwardMessage()
    {
        var messageForwardReceivedManualEvent = new ManualResetEvent(false);

        var forwardingChannel = chat.CreatePublicConversation("forwarding_channel");
        forwardingChannel.OnMessageReceived += message =>
        {
            Assert.True(message.MessageText == "message_to_forward");
            messageForwardReceivedManualEvent.Set();
        };
        forwardingChannel.Join();

        channel.Join();
        channel.OnMessageReceived += message => { chat.ForwardMessage(message, forwardingChannel); };

        channel.SendText("message_to_forward");

        var forwarded = messageForwardReceivedManualEvent.WaitOne(6000);
        Assert.True(forwarded);
    }

    [Test]
    public void TestEmitEvent()
    {
        var reportManualEvent = new ManualResetEvent(false);
        chat.OnReportEvent += reportEvent =>
        {
            Assert.True(reportEvent.Payload == "{\"test\":\"some_nonsense\", \"type\": \"report\"}");
            reportManualEvent.Set();
        };
        channel.Join();
        chat.EmitEvent(PubnubChatEventType.Report, channel.Id, "{\"test\":\"some_nonsense\"}");

        var eventReceived = reportManualEvent.WaitOne(5000);
        Assert.True(eventReceived);
    }

    [Test]
    public async Task TestGetUnreadMessagesCounts()
    {
        channel.SendText("wololo");

        await Task.Delay(3000);

        Assert.True(chat.GetUnreadMessagesCounts(limit: 50).Any(x => x.Channel.Id == channel.Id && x.Count > 0));
    }

    [Test]
    public async Task TestMarkAllMessagesAsRead()
    {
        channel.SendText("wololo");

        await Task.Delay(3000);

        Assert.True(chat.GetUnreadMessagesCounts().Any(x => x.Channel.Id == channel.Id && x.Count > 0));

        var res = chat.MarkAllMessagesAsRead();

        await Task.Delay(5000);

        var counts = chat.GetUnreadMessagesCounts();

        Assert.False(counts.Any(x => x.Count > 0));
    }

    [Test]
    public async Task TestReadReceipts()
    {
        var otherChat = new Chat(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "other_chat_user")
        );
        if (!otherChat.TryGetChannel(channel.Id, out var otherChatChannel))
        {
            Assert.Fail();
            return;
        }

        otherChatChannel.Join();

        var receiptReset = new ManualResetEvent(false);
        otherChat.OnReadReceiptEvent += receiptEvent =>
        {
            Assert.True(receiptEvent.ChannelId == channel.Id && receiptEvent.UserId == user.Id);
            receiptReset.Set();
        };
        otherChatChannel.SendText("READ MEEEE");

        await Task.Delay(5000);

        chat.MarkAllMessagesAsRead();
        var receipt = receiptReset.WaitOne(15000);
        Assert.True(receipt);
    }

    [Test]
    public async Task TestCanI()
    {
        await Task.Delay(4000);
        
        var accessChat = new Chat(
            new PubnubChatConfig(
                PubnubTestsParameters.PublishKey,
                PubnubTestsParameters.SubscribeKey,
                "can_i_test_user",
                authKey: "qEF2AkF0Gma8TDFDdHRsGX0AQ3Jlc6VEY2hhbqFyY2FuX2lfdGVzdF9jaGFubmVsEUNncnCgQ3NwY6BDdXNyoER1dWlkoW9jYW5faV90ZXN0X3VzZXIY_0NwYXSlRGNoYW6gQ2dycKBDc3BjoEN1c3KgRHV1aWSgRG1ldGGgRHV1aWRvY2FuX2lfdGVzdF91c2VyQ3NpZ1ggAEijACv1wHoiwQulMhEPFRKEb1C4MYIgfS0wyYMCj3Y="
                ));
        Assert.False(accessChat.ChatAccessManager.CanI(PubnubAccessPermission.Write, PubnubAccessResourceType.Channels,
            "can_i_test_channel"));
        Assert.True(accessChat.ChatAccessManager.CanI(PubnubAccessPermission.Read, PubnubAccessResourceType.Channels,
            "can_i_test_channel"));
    }
}