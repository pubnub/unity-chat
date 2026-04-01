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
        chat = TestUtils.AssertOperation(await Chat.CreateInstance(
            new PubnubChatConfig(
                storeUserActivityTimestamp: true, 
                emitReadReceiptEvents:new Dictionary<string, bool>()
                    {
                        {"public", true},
                        {"group", true},
                        {"direct", true},
                    }), 
            new PNConfiguration(new UserId("chats_tests_user_fresh_3"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        }));
        channel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        currentUser = TestUtils.AssertOperation(await chat.GetCurrentUser());
        await channel.Join();
        await Task.Delay(3500);
    }
    
    [TearDown]
    public async Task CleanUp()
    {
        await channel.Leave();
        await Task.Delay(1000);
        await channel.Delete();
        await Task.Delay(1000);
        chat.Destroy();
        await Task.Delay(1000);
    }
    
    [Test]
    public async Task TestGetCurrentUserMentions()
    {
        var messageContent = "wololo";
        var draft = channel.CreateMessageDraft();
        draft.InsertText(0, messageContent);
        draft.AddMention(0, 6, new MentionTarget()
        {
            Type = MentionType.User,
            Target = currentUser.Id
        });
        await draft.Send();

        await Task.Delay(3000);

        var mentions = TestUtils.AssertOperation(await chat.GetCurrentUserMentions("99999999999999999", "00000000000000000", 10));
        
        Assert.True(mentions != null);
        Assert.True(mentions.Mentions.Any(x => x.ChannelId == channel.Id && x.Message.MessageText.Contains(messageContent)));
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
        await testChannel.EmitCustomEvent("{\"test\":\"some_nonsense\"}");

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
    public async Task TestStatusListener()
    {
        var testChannel = TestUtils.AssertOperation(await chat.CreatePublicConversation());
        
        chat.StreamSubscriptionStatus(true);
        await Task.Delay(2500);
        
        var connectedReset = new ManualResetEvent(false);
        chat.OnSubscriptionStatusChanged += (status) =>
        {
            if (status.Category == PNStatusCategory.PNSubscriptionChangedCategory &&
                status.AffectedChannels.Contains(testChannel.Id))
            {
                connectedReset.Set();   
            }
        };
        
        testChannel.Connect();
        
        var receivedConnected = connectedReset.WaitOne(15000);
        Assert.True(receivedConnected, "Didn't receive PNSubscriptionChangedCategory status");
        
        var disconnectedReset = new ManualResetEvent(false);
        chat.OnSubscriptionStatusChanged += (status) =>
        {
            if (status.Category == PNStatusCategory.PNSubscriptionChangedCategory &&
                !status.AffectedChannels.Contains(testChannel.Id))
            {
                disconnectedReset.Set();   
            }
        };
        testChannel.Disconnect();
        
        var receivedDisconnected = disconnectedReset.WaitOne(15000);
        Assert.True(receivedDisconnected, "Didn't receive PNSubscriptionChangedCategory status");
        
        chat.StreamSubscriptionStatus(false);
        await Task.Delay(2000);
        
        connectedReset = new ManualResetEvent(false);
        testChannel.Connect();
        
        receivedConnected = connectedReset.WaitOne(5000);
        Assert.False(receivedConnected, "Received PNSubscriptionChangedCategory despite unsubscribing");
        
        disconnectedReset = new ManualResetEvent(false);
        testChannel.Disconnect();

        receivedDisconnected = disconnectedReset.WaitOne(15000);
        Assert.False(receivedDisconnected, "Received PNSubscriptionChangedCategory despite unsubscribing");
        
        //Cleanup
        await testChannel.Delete();
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

        var unread = TestUtils.AssertOperation(await chat.GetUnreadMessagesCounts(filter:$"channel.id LIKE \"{markTestChannel.Id}\""));
        Assert.True(unread.Any(x => x.ChannelId == markTestChannel.Id && x.Count > 0));

        TestUtils.AssertOperation(await chat.MarkAllMessagesAsRead(filter:$"channel.id LIKE \"{markTestChannel.Id}\""));

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
        otherChatChannel.OnReadReceiptEvent += readReceipt =>
        {
            Assert.True(readReceipt.UserId == currentUser.Id);
            receiptReset.Set();
        };
        await otherChatChannel.SendText("READ MEEEE");

        await Task.Delay(5000);

        await chat.MarkAllMessagesAsRead(filter:$"channel.id LIKE \"{channel.Id}\"");
        var receipt = receiptReset.WaitOne(15000);
        Assert.True(receipt);
    }
    
    [Test]
    public async Task TestDisableReadReceiptsInChatConfig()
    {
        var otherUserId = "other_chat_user";
        var otherChat = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(
                storeUserActivityTimestamp: true, 
                emitReadReceiptEvents: new Dictionary<string, bool>()
                {
                    {"public", false}
                }), 
            new PNConfiguration(new UserId(otherUserId))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        }));
        var otherChatChannel = TestUtils.AssertOperation(await otherChat.GetChannel(channel.Id));
        await otherChatChannel.Join();
        await Task.Delay(2500);
        
        channel.StreamReadReceipts(true);
        await Task.Delay(2500);

        var receiptReset = new ManualResetEvent(false);
        channel.OnReadReceiptEvent += readReceipt =>
        {
            Assert.True(readReceipt.UserId == otherUserId);
            receiptReset.Set();
        };
        await channel.SendText("READ MEEEE");

        await Task.Delay(5000);

        await otherChat.MarkAllMessagesAsRead(filter:$"channel.id LIKE \"{channel.Id}\"");
        var receipt = receiptReset.WaitOne(15000);
        Assert.False(receipt, "Received read receipt even with config set not to send events");
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