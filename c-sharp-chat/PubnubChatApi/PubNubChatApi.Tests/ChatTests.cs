using System.Diagnostics;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Enums;

namespace PubNubChatApi.Tests;

public class ChatTests
{
    private Chat chat;
    private Channel channel;
    private User currentUser;

    [SetUp]
    public async Task Setup()
    {
        chat = await Chat.CreateInstance(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "chats_tests_user_10_no_calkiem_nowy_2"));
        channel = await chat.OLD_CreatePublicConversation("chat_tests_channel_2");
        if (!chat.OLD_TryGetCurrentUser(out currentUser))
        {
            Assert.Fail();
        }
        channel.Join();
        await Task.Delay(3500);
    }
    
    [TearDown]
    public async Task CleanUp()
    {
        channel.Leave();
        await Task.Delay(1000);
        chat.Destroy();
        await Task.Delay(1000);
    }
    
    [Test]
    public async Task TestGetCurrentUserMentions()
    {
        var messageContent = "wololo";
        await channel.SendText(messageContent, new SendTextParams()
        {
            MentionedUsers = new Dictionary<int, User>()
            {
                {0, currentUser}
            }
        });

        await Task.Delay(3000);

        var mentions = await chat.GetCurrentUserMentions("99999999999999999", "00000000000000000", 10);
        
        Assert.True(mentions != null);
        Assert.True(mentions.Mentions.Any(x => x.ChannelId == channel.Id && x.Message.MessageText == messageContent));
    }

    [Test]
    public async Task TestGetCurrentUser()
    {
        Assert.True(chat.OLD_TryGetCurrentUser(out var currentUser) && currentUser.Id == this.currentUser.Id);
    }

    [Test]
    public async Task TestGetEventHistory()
    {
        await chat.EmitEvent(PubnubChatEventType.Custom, channel.Id, "{\"test\":\"some_nonsense\"}");

        await Task.Delay(5000);

        var history = await chat.GetEventsHistory(channel.Id, "99999999999999999", "00000000000000000", 50);
        Assert.True(history.Events.Any(x => x.ChannelId == channel.Id));
    }

    [Test]
    public async Task TestGetUsers()
    {
        var users = await chat.OLD_GetUsers();
        Assert.True(users.Users.Any());
    }

    [Test]
    public async Task TestGetChannels()
    {
        await Task.Delay(4000);
        var channels = await chat.GetChannels();
        Assert.True(channels.Channels.Any());
    }

    [Test]
    public async Task TestCreateDirectConversation()
    {
        var convoUser = await chat.GetOrCreateUser("direct_conversation_user");
        var directConversation =
            await chat.OLD_CreateDirectConversation(convoUser, "direct_conversation_test");
        Assert.True(directConversation.CreatedChannel is { Id: "direct_conversation_test" });
        Assert.True(directConversation.HostMembership != null && directConversation.HostMembership.UserId == currentUser.Id);
        Assert.True(directConversation.InviteesMemberships != null &&
                    directConversation.InviteesMemberships.First().UserId == convoUser.Id);
    }

    [Test]
    public async Task TestCreateGroupConversation()
    {
        var convoUser1 = await chat.GetOrCreateUser("group_conversation_user_1");
        var convoUser2 = await chat.GetOrCreateUser("group_conversation_user_2");
        var convoUser3 = await chat.GetOrCreateUser("group_conversation_user_3");
        var groupConversation = await 
            chat.OLD_CreateGroupConversation([convoUser1, convoUser2, convoUser3], "group_conversation_test");
        Assert.True(groupConversation.CreatedChannel is { Id: "group_conversation_test" });
        Assert.True(groupConversation.HostMembership != null && groupConversation.HostMembership.UserId == currentUser.Id);
        Assert.True(groupConversation.InviteesMemberships is { Count: 3 });
        Assert.True(groupConversation.InviteesMemberships.Any(x =>
            x.UserId == convoUser1.Id && x.ChannelId == "group_conversation_test"));
    }

    [Test]
    public async Task TestForwardMessage()
    {
        var messageForwardReceivedManualEvent = new ManualResetEvent(false);

        var forwardingChannel = await chat.OLD_CreatePublicConversation("forwarding_channel");
        forwardingChannel.OnMessageReceived += message =>
        {
            Assert.True(message.MessageText == "message_to_forward");
            messageForwardReceivedManualEvent.Set();
        };
        forwardingChannel.Join();
        await Task.Delay(2500);
        
        channel.OnMessageReceived += async message => { await chat.ForwardMessage(message, forwardingChannel); };

        await channel.SendText("message_to_forward");

        var forwarded = messageForwardReceivedManualEvent.WaitOne(6000);
        Assert.True(forwarded);
    }

    [Test]
    public async Task TestEmitEvent()
    {
        var reportManualEvent = new ManualResetEvent(false);
        channel.OnCustomEvent += customEvent =>
        {
            Assert.True(customEvent.Payload == "{\"test\":\"some_nonsense\", \"type\": \"custom\"}");
            reportManualEvent.Set();
        };
        channel.SetListeningForCustomEvents(true);
        await Task.Delay(2500);
        await chat.EmitEvent(PubnubChatEventType.Custom, channel.Id, "{\"test\":\"some_nonsense\"}");

        var eventReceived = reportManualEvent.WaitOne(8000);
        Assert.True(eventReceived);
    }

    [Test]
    public async Task TestGetUnreadMessagesCounts()
    {
        await channel.SendText("wololo");

        await Task.Delay(3000);

        Assert.True((await chat.GetUnreadMessagesCounts(limit: 50)).Any(x => x.Channel.Id == channel.Id && x.Count > 0));
    }

    [Test]
    public async Task TestMarkAllMessagesAsRead()
    {
        await channel.SendText("wololo");

        await Task.Delay(10000);

        Assert.True((await chat.GetUnreadMessagesCounts()).Any(x => x.Channel.Id == channel.Id && x.Count > 0));

        var res = chat.MarkAllMessagesAsRead();

        await Task.Delay(2000);

        var counts = await chat.GetUnreadMessagesCounts();

        Assert.False(counts.Any(x => x.Channel.Id == channel.Id && x.Count > 0));
    }

    [Test]
    public async Task TestReadReceipts()
    {
        var otherChat = await Chat.CreateInstance(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "other_chat_user")
        );
        if (!otherChat.OLD_TryGetChannel(channel.Id, out var otherChatChannel))
        {
            Assert.Fail();
            return;
        }

        otherChatChannel.Join();
        await Task.Delay(2500);
        otherChatChannel.SetListeningForReadReceiptsEvents(true);
        await Task.Delay(2500);

        var receiptReset = new ManualResetEvent(false);
        otherChatChannel.OnReadReceiptEvent += readReceipts =>
        {
            if (readReceipts.Count == 0)
            {
                return;
            }
            Assert.True(readReceipts.Values.Any(x => x != null && x.Contains(currentUser.Id)));
            receiptReset.Set();
        };
        await otherChatChannel.SendText("READ MEEEE");

        await Task.Delay(5000);

        await chat.MarkAllMessagesAsRead();
        var receipt = receiptReset.WaitOne(15000);
        Assert.True(receipt);
    }

    [Test]
    public async Task TestCanI()
    {
        await Task.Delay(4000);
        
        var accessChat = await Chat.CreateInstance(
            new PubnubChatConfig(
                PubnubTestsParameters.PublishKey,
                PubnubTestsParameters.SubscribeKey,
                "can_i_test_user",
                authKey: "qEF2AkF0Gma8TDFDdHRsGX0AQ3Jlc6VEY2hhbqFyY2FuX2lfdGVzdF9jaGFubmVsEUNncnCgQ3NwY6BDdXNyoER1dWlkoW9jYW5faV90ZXN0X3VzZXIY_0NwYXSlRGNoYW6gQ2dycKBDc3BjoEN1c3KgRHV1aWSgRG1ldGGgRHV1aWRvY2FuX2lfdGVzdF91c2VyQ3NpZ1ggAEijACv1wHoiwQulMhEPFRKEb1C4MYIgfS0wyYMCj3Y="
                ));
        Assert.False(await accessChat.ChatAccessManager.CanI(PubnubAccessPermission.Write, PubnubAccessResourceType.Channels,
            "can_i_test_channel"));
        Assert.True(await accessChat.ChatAccessManager.CanI(PubnubAccessPermission.Read, PubnubAccessResourceType.Channels,
            "can_i_test_channel"));
    }
}