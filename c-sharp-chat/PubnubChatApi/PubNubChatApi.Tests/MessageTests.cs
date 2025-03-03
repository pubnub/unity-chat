using System.Diagnostics;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

public class MessageTests
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
            "message_tests_user")
        );
        channel = await chat.CreatePublicConversation("message_tests_channel_2");
        if (!chat.TryGetCurrentUser(out user))
        {
            Assert.Fail();
        }
        channel.Join();
        await Task.Delay(2500);
    }
    
    [TearDown]
    public async Task CleanUp()
    {
        channel.Leave();
        await Task.Delay(3000);
        chat.Destroy();
        await Task.Delay(3000);
    }

    [Test]
    public async Task TestSendAndReceive()
    {
        var manualReceiveEvent = new ManualResetEvent(false);

        channel.OnMessageReceived += message =>
        {
            Assert.True(message.MessageText == "Test message text");
            manualReceiveEvent.Set();
        };
        await channel.SendText("Test message text", new SendTextParams()
        {
            MentionedUsers = new Dictionary<int, User>() { { 0, user } },
        });
        var received = manualReceiveEvent.WaitOne(6000);
        Assert.IsTrue(received);
    }

    [Test]
    public async Task TestReceivingMessageData()
    {
        var manualReceiveEvent = new ManualResetEvent(false);
        var testChannel = await chat.CreatePublicConversation("message_data_test_channel");
        testChannel.Join();
        await Task.Delay(2500);
        testChannel.OnMessageReceived += async message =>
        {
            if (message.MessageText == "message_to_be_quoted")
            {
                await testChannel.SendText("message_with_data", new SendTextParams()
                {
                    MentionedUsers = new Dictionary<int, User>() { { 0, user } },
                    QuotedMessage = message
                });
            }
            else if (message.MessageText == "message_with_data")
            {
                Assert.True(message.MentionedUsers.Any(x => x.Id == user.Id));
                Assert.True(message.TryGetQuotedMessage(out var quotedMessage) &&
                            quotedMessage.MessageText == "message_to_be_quoted");
                manualReceiveEvent.Set();
            }
        };
        await testChannel.SendText("message_to_be_quoted");

        var received = manualReceiveEvent.WaitOne(9000);
        Assert.IsTrue(received);
    }

    [Test]
    public async Task TestTryGetMessage()
    {
        var manualReceiveEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += message =>
        {
            if (message.ChannelId == channel.Id)
            {
                Assert.True(chat.TryGetMessage(channel.Id, message.TimeToken, out _));
                manualReceiveEvent.Set();
            }
        };
        await channel.SendText("something");

        var received = manualReceiveEvent.WaitOne(4000);
        Assert.IsTrue(received);
    }

    [Test]
    public async Task TestEditMessage()
    {
        var manualUpdatedEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            message.SetListeningForUpdates(true);
            await Task.Delay(2000);
            
            message.OnMessageUpdated += updatedMessage =>
            {
                manualUpdatedEvent.Set();
                Assert.True(updatedMessage.MessageText == "new-text");
            };
            await message.EditMessageText("new-text");
        };
        await channel.SendText("something");

        var receivedAndUpdated = manualUpdatedEvent.WaitOne(14000);
        Assert.IsTrue(receivedAndUpdated);
    }

    [Test]
    public async Task TestDeleteMessage()
    {
        var manualReceivedEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            message.Delete(true);

            await Task.Delay(2000);

            Assert.True(message.IsDeleted);
            manualReceivedEvent.Set();
        };
        await channel.SendText("something");

        var received = manualReceivedEvent.WaitOne(4000);
        Assert.IsTrue(received);
    }

    [Test]
    public async Task TestRestoreMessage()
    {
        var manualReceivedEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            await message.Delete(true);
            Assert.True(message.IsDeleted);

            await Task.Delay(4000);
            
            Assert.True(message.IsDeleted);
            await message.Restore();

            await Task.Delay(4000);

            Assert.False(message.IsDeleted);
            manualReceivedEvent.Set();
        };
        await channel.SendText("some text here ladi ladi la");

        var received = manualReceivedEvent.WaitOne(25000);
        Assert.IsTrue(received);
    }

    [Test]
    public async Task TestPinMessage()
    {
        if (chat.TryGetChannel("pin_test_2", out var existingChannel))
        {
            await chat.DeleteChannel(existingChannel.Id);
            await Task.Delay(4000);
        }

        var pinTestChannel = await chat.CreatePublicConversation("pin_test_2");
        pinTestChannel.Join();
        pinTestChannel.SetListeningForUpdates(true);
        await Task.Delay(3000);

        var manualReceivedEvent = new ManualResetEvent(false);
        pinTestChannel.OnMessageReceived += async message =>
        {
            await message.Pin();

            await Task.Delay(3000);

            var got = pinTestChannel.TryGetPinnedMessage(out var pinnedMessage);
            Assert.True(got && pinnedMessage.MessageText == "message to pin");
            manualReceivedEvent.Set();
        };
        await pinTestChannel.SendText("message to pin");

        var received = manualReceivedEvent.WaitOne(12000);
        Assert.IsTrue(received);
    }

    [Test]
    public async Task TestMessageReactions()
    {
        var manualReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            await message.ToggleReaction("happy");

            await Task.Delay(3000);

            var has = message.HasUserReaction("happy");
            Assert.True(has);
            var reactions = message.Reactions;
            Assert.True(reactions.Count == 1 && reactions.Any(x => x.Value == "happy"));
            manualReset.Set();
        };
        await channel.SendText("a_message");
        var reacted = manualReset.WaitOne(10000);
        Assert.True(reacted);
    }

    [Test]
    public async Task TestMessageReport()
    {
        var reportManualEvent = new ManualResetEvent(false);
        channel.Join();
        channel.SetListeningForReportEvents(true);
        await Task.Delay(3000);
        channel.OnReportEvent += reportEvent =>
        {
            Assert.True(reportEvent.Payload.Contains("bad_message"));
            reportManualEvent.Set();
        };
        channel.OnMessageReceived += async message => { await message.Report("bad_message"); };
        await channel.SendText("message_to_be_reported");
        var reported = reportManualEvent.WaitOne(12000);
        Assert.True(reported);
    }

    [Test]
    public async Task TestCreateThread()
    {
        var manualReceiveEvent = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var hasThread = false;
            try
            {
                var thread = await message.CreateThread();
                await Task.Delay(2000);
                await thread.SendText("thread_init_text");
                await Task.Delay(2000);
                hasThread = message.HasThread();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Assert.Fail();
            }

            Assert.True(hasThread);
            Assert.True(message.TryGetThread(out var threadChannel));
            await message.RemoveThread();

            await Task.Delay(5000);

            //TODO: temporary way to get latest message pointer since remove_thread doesn't return a new pointer
            chat.TryGetMessage(channel.Id, message.Id, out message);
            Assert.False(message.HasThread());

            manualReceiveEvent.Set();
        };
        await channel.SendText("thread_start_message");

        var received = manualReceiveEvent.WaitOne(25000);
        Assert.IsTrue(received);
    }
}