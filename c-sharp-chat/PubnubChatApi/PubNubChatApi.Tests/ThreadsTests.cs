using System.Diagnostics;
using PubnubApi;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using Channel = PubNubChatAPI.Entities.Channel;

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
        chat = new Chat(new PubnubChatConfig(storeUserActivityTimestamp: true), new PNConfiguration(new UserId("threads_tests_user_2"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        });
        var randomId = Guid.NewGuid().ToString()[..10];
        channel = await chat.CreatePublicConversation(randomId);
        if (!chat.TryGetCurrentUser(out user))
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
        await Task.Delay(3000);
        await channel.Delete();
        chat.Destroy();
        await Task.Delay(3000);
    }

    [Test]
    public async Task TestGetThreadHistory()
    {
        var historyReadReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            message.SetListeningForUpdates(true);
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
            message.SetListeningForUpdates(true);
            var thread = await message.CreateThread();
            thread.Join();
            await thread.SendText("thread init message");

            await Task.Delay(7000);

            var threadMessage = 
                (await thread.GetThreadHistory("99999999999999999", "00000000000000000", 1))[0];
            await thread.PinMessageToParentChannel(threadMessage);
            
            await Task.Delay(7000);
            
            var hasPinned = channel.TryGetPinnedMessage(out var pinnedMessage);
            var correctText = hasPinned && pinnedMessage.MessageText == "thread init message";
            Assert.True(hasPinned && correctText);
            await thread.UnPinMessageFromParentChannel();
            
            await Task.Delay(7000);

            Assert.False(channel.TryGetPinnedMessage(out _));
            historyReadReset.Set();
        };
        await channel.SendText("thread_start_message");
        var read = historyReadReset.WaitOne(40000);
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
            await Task.Delay(2500);
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
            message.SetListeningForUpdates(true);
            var thread = await message.CreateThread();
            thread.Join();

            await Task.Delay(3500);
            
            await thread.SendText("one");
            await thread.SendText("two");
            await thread.SendText("three");
            
            await Task.Delay(8000);
            
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
            message.SetListeningForUpdates(true);
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
        var updated = messageUpdatedReset.WaitOne(25000);
        Assert.True(updated);
    }
}