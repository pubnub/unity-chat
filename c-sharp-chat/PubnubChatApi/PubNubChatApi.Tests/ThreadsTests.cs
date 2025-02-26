using System.Diagnostics;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

[TestFixture]
public class ThreadsTests
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
            "threads_tests_user")
        );
        channel = await chat.CreatePublicConversation("threads_tests_channel_37");
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
    public async Task TestGetThreadHistory()
    {
        var historyReadReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var thread = await message.CreateThread();
            thread.Join();

            await Task.Delay(5000);
            
            await thread.SendText("one");
            await thread.SendText("two");
            await thread.SendText("three");

            await Task.Delay(10000);

            var history = await thread.GetThreadHistory("99999999999999999", "00000000000000000", 3);
            Assert.True(history.Count == 3 && history.Any(x => x.MessageText == "one"));
            historyReadReset.Set();
        };
        await channel.SendText("thread_start_message");
        var read = historyReadReset.WaitOne(50000);
        Assert.True(read);
    }

    [Test]
    public async Task TestThreadChannelParentChannelPinning()
    {
        var historyReadReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var thread = await message.CreateThread();
            thread.Join();
            await Task.Delay(2500);
            await thread.SendText("thread init message");

            await Task.Delay(5000);
            
            thread.OnMessageReceived += async threadMessage =>
            {
                await thread.PinMessageToParentChannel(threadMessage);
            };
            await thread.SendText("some_thread_message");
            
            await Task.Delay(5000);

            Assert.True(channel.TryGetPinnedMessage(out var pinnedMessage) && pinnedMessage.MessageText == "some_thread_message");
            await thread.UnPinMessageFromParentChannel();
            
            await Task.Delay(5000);

            Assert.False(channel.TryGetPinnedMessage(out _));
            historyReadReset.Set();
        };
        await channel.SendText("thread_start_message");
        var read = historyReadReset.WaitOne(30000);
        Assert.True(read);
    }
    
    [Test]
    public async Task TestThreadChannelEmitUserMention()
    {
        var mentionedReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var thread = await message.CreateThread();
            thread.Join();
            user.SetListeningForMentionEvents(true);
            await Task.Delay(2500);
            user.OnMentionEvent += mentionEvent =>
            {
                Assert.True(mentionEvent.Payload.Contains("heyyy"));
                mentionedReset.Set();
            };
            await thread.EmitUserMention(user.Id, "99999999999999999", "heyyy");
        };
        await channel.SendText("thread_start_message");
        var read = mentionedReset.WaitOne(10000);
        Assert.True(read);
    }
    
    [Test]
    public async Task TestThreadMessageParentChannelPinning()
    {
        var historyReadReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var thread = await message.CreateThread();
            thread.Join();

            await Task.Delay(3000);
            
            await thread.SendText("one");
            await thread.SendText("two");
            await thread.SendText("three");
            
            await Task.Delay(10000);
            
            var history = await thread.GetThreadHistory("99999999999999999", "00000000000000000", 3);
            var threadMessage = history[0];
            await threadMessage.PinMessageToParentChannel();
            
            await Task.Delay(5000);

            Assert.True(channel.TryGetPinnedMessage(out var pinnedMessage) && pinnedMessage.MessageText == threadMessage.MessageText);
            await threadMessage.UnPinMessageFromParentChannel();
            
            await Task.Delay(5000);

            Assert.False(channel.TryGetPinnedMessage(out _));
            historyReadReset.Set();
        };
        await channel.SendText("thread_start_message");
        var read = historyReadReset.WaitOne(45000);
        Assert.True(read);
    }

    [Test]
    public async Task TestThreadMessageUpdate()
    {
        var messageUpdatedReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var thread = await message.CreateThread();
            thread.Join();
            
            await Task.Delay(3000);
            
            await thread.SendText("one");
            await thread.SendText("two");
            await thread.SendText("three");
            
            await Task.Delay(10000);
            
            var history = await thread.GetThreadHistory("99999999999999999", "00000000000000000", 3);
            var threadMessage = history[0];
            
            threadMessage.SetListeningForUpdates(true);
            threadMessage.OnThreadMessageUpdated += updatedThreadMessage =>
            {
                Assert.True(updatedThreadMessage.MessageText == "new_text");
                messageUpdatedReset.Set();
            };
            await threadMessage.EditMessageText("new_text");
        };
        await channel.SendText("thread_start_message");
        var updated = messageUpdatedReset.WaitOne(255000);
        Assert.True(updated);
    }
}