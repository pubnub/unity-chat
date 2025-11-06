using PubnubApi;
using PubnubChatApi;
using Channel = PubnubChatApi.Channel;

namespace PubNubChatApi.Tests;

public class ChatTests
{
    private Chat chat;
    private Channel channel;
    private User currentUser;

    [SetUp]
    public async Task Setup()
    {
        chat = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(storeUserActivityTimestamp: true), 
            new PNConfiguration(new UserId("chats_tests_user_fresh_3"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        }));
        channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("chat_tests_channel_2"));
        currentUser = TestUtils.AssertOperation(await chat.GetCurrentUser());
        await channel.Join();
        await Task.Delay(3500);
    }
    
    [TearDown]
    public async Task CleanUp()
    {
        await channel.Leave();
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
            MentionedUsers = new Dictionary<int, MentionedUser>()
            {
                {0, new MentionedUser()
                {
                    Id = currentUser.Id,
                    Name = currentUser.UserName
                }}
            }
        });

        await Task.Delay(3000);

        var mentions = TestUtils.AssertOperation(await chat.GetCurrentUserMentions("99999999999999999", "00000000000000000", 10));
        
        Assert.True(mentions != null);
        Assert.True(mentions.Mentions.Any(x => x.ChannelId == channel.Id && x.Message.MessageText == messageContent));
    }

    [Test]
    public async Task TestGetCurrentUser()
    {
        var fetchedCurrentUser = TestUtils.AssertOperation(await chat.GetCurrentUser());
        Assert.True(fetchedCurrentUser.Id == currentUser.Id);
    }

    [Test]
    public async Task TestGetEventHistory()
    {
        var testChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        await chat.EmitEvent(PubnubChatEventType.Custom, testChannel.Id, "{\"test\":\"some_nonsense\"}");

        await Task.Delay(5000);

        var history = TestUtils.AssertOperation(
            await chat.GetEventsHistory(testChannel.Id, "99999999999999999", "00000000000000000", 50));
        Assert.True(history.Events.Any(x => x.ChannelId == testChannel.Id && x.Payload.Contains("\"test\":\"some_nonsense\"")), 
            "Emitted event wasn't present in events history");
    }

    [Test]
    public async Task TestGetUsers()
    {
        var users = TestUtils.AssertOperation(await chat.GetUsers());
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
        var id = Guid.NewGuid().ToString();
        var directConversation = TestUtils.AssertOperation(
            await chat.CreateDirectConversation(convoUser, id));
        Assert.True(directConversation.CreatedChannel.Id == id);
        Assert.True(directConversation.HostMembership != null && directConversation.HostMembership.UserId == currentUser.Id);
        Assert.True(directConversation.InviteesMemberships != null &&
                    directConversation.InviteesMemberships.First().UserId == convoUser.Id);

        //Cleanup
        await directConversation.CreatedChannel.Delete(false);
    }

    [Test]
    public async Task TestCreateGroupConversation()
    {
        var convoUser1 = await chat.GetOrCreateUser("group_conversation_user_1");
        var convoUser2 = await chat.GetOrCreateUser("group_conversation_user_2");
        var convoUser3 = await chat.GetOrCreateUser("group_conversation_user_3");
        var id = Guid.NewGuid().ToString();
        var groupConversation = TestUtils.AssertOperation(await 
            chat.CreateGroupConversation([convoUser1, convoUser2, convoUser3], id));
        Assert.True(groupConversation.CreatedChannel.Id == id);
        Assert.True(groupConversation.HostMembership != null && groupConversation.HostMembership.UserId == currentUser.Id);
        Assert.True(groupConversation.InviteesMemberships is { Count: 3 });
        Assert.True(groupConversation.InviteesMemberships.Any(x =>
            x.UserId == convoUser1.Id && x.ChannelId == id));
        
        //Cleanup
        await groupConversation.CreatedChannel.Delete(false);
    }

    [Test]
    public async Task TestForwardMessage()
    {
        var messageForwardReceivedManualEvent = new ManualResetEvent(false);

        var forwardingChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation("forwarding_channel"));
        forwardingChannel.OnMessageReceived += message =>
        {
            Assert.True(message.MessageText == "message_to_forward");
            messageForwardReceivedManualEvent.Set();
        };
        await forwardingChannel.Join();
        await Task.Delay(2500);
        
        channel.OnMessageReceived += async message => { await message.Forward(forwardingChannel.Id); };

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
            Assert.True(customEvent.Payload.Contains("test"));
            Assert.True(customEvent.Payload.Contains("some_nonsense"));
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
        var testChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        await testChannel.Join();
        await testChannel.SendText("wololo");
        await testChannel.SendText("wololo1");
        await testChannel.SendText("wololo2");
        await testChannel.SendText("wololo3");

        await Task.Delay(6000);

        var unreads =
            TestUtils.AssertOperation(await chat.GetUnreadMessagesCounts(filter:$"channel.id LIKE \"{testChannel.Id}\""));
        Assert.True(unreads.Any(x => x.ChannelId == testChannel.Id && x.Count == 4));

        await testChannel.Delete(false);
    }

    [Test]
    public async Task TestMarkAllMessagesAsRead()
    {
        var markTestChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        await markTestChannel.Join();
        
        await Task.Delay(4000);
        
        await markTestChannel.SendText("wololo", new SendTextParams(){StoreInHistory = true});

        await Task.Delay(4000);

        var unread = TestUtils.AssertOperation(await chat.GetUnreadMessagesCounts());
        foreach (var wrapper in unread)
        {
            Console.WriteLine($"{wrapper.ChannelId}:{wrapper.Count}");
        }
        Assert.True(unread.Any(x => x.ChannelId == markTestChannel.Id && x.Count > 0));

        TestUtils.AssertOperation(await chat.MarkAllMessagesAsRead());

        await Task.Delay(7000);
        
        var counts = TestUtils.AssertOperation(await chat.GetUnreadMessagesCounts());

        await markTestChannel.Leave();
        await markTestChannel.Delete(false);
        
        Assert.False(counts.Any(x => x.ChannelId == markTestChannel.Id && x.Count > 0));
    }

    [Test]
    public async Task TestReadReceipts()
    {
        var otherChat = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(storeUserActivityTimestamp: true), new PNConfiguration(new UserId("other_chat_user"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        }));
        var otherChatChannel = TestUtils.AssertOperation(await otherChat.GetChannel(channel.Id));

        await otherChatChannel.Join();
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
        
        var accessChat = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(storeUserActivityTimestamp: true), new PNConfiguration(new UserId("can_i_test_user"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey,
            AuthKey = "qEF2AkF0Gma8TDFDdHRsGX0AQ3Jlc6VEY2hhbqFyY2FuX2lfdGVzdF9jaGFubmVsEUNncnCgQ3NwY6BDdXNyoER1dWlkoW9jYW5faV90ZXN0X3VzZXIY_0NwYXSlRGNoYW6gQ2dycKBDc3BjoEN1c3KgRHV1aWSgRG1ldGGgRHV1aWRvY2FuX2lfdGVzdF91c2VyQ3NpZ1ggAEijACv1wHoiwQulMhEPFRKEb1C4MYIgfS0wyYMCj3Y="
        }));
        Assert.False(await accessChat.ChatAccessManager.CanI(PubnubAccessPermission.Write, PubnubAccessResourceType.Channels,
            "can_i_test_channel"));
        Assert.True(await accessChat.ChatAccessManager.CanI(PubnubAccessPermission.Read, PubnubAccessResourceType.Channels,
            "can_i_test_channel"));
    }
}