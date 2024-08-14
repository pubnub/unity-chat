using System.Diagnostics;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

public class ThreadsTests
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
            "threads_tests_user")
        );
        channel = chat.CreatePublicConversation("threads_tests_channel_37");
        channel.Join();
    }

    [Test]
    public void TestGetThreadHistory()
    {
        var historyReadReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var thread = message.CreateThread();
            thread.SendText("one");
            thread.SendText("two");
            thread.SendText("three");

            await Task.Delay(6000);

            var history = thread.GetThreadHistory("99999999999999999", "00000000000000000", 3);
            Assert.True(history.Count == 3 && history.Any(x => x.MessageText == "one"));
            historyReadReset.Set();
        };
        channel.SendText("thread_start_message");
        var read = historyReadReset.WaitOne(12000);
        Assert.True(read);
    }

    [Test]
    public void TestThreadChannelParentChannelPinning()
    {
        var historyReadReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var thread = message.CreateThread();
            thread.Join();
            thread.OnMessageReceived += threadMessage =>
            {
                //TODO: those will fail if the ThreadChannel true updates are enabled
                Debug.WriteLine(thread.ParentChannelId);
                thread.PinMessageToParentChannel(threadMessage);
            };
            thread.SendText("some_thread_message");
            
            await Task.Delay(5000);

            Assert.True(channel.TryGetPinnedMessage(out var pinnedMessage) && pinnedMessage.MessageText == "some_thread_message");
            thread.UnPinMessageFromParentChannel();
            
            await Task.Delay(5000);

            Assert.False(channel.TryGetPinnedMessage(out _));
            historyReadReset.Set();
        };
        channel.SendText("thread_start_message");
        var read = historyReadReset.WaitOne(15000);
        Assert.True(read);
    }
    
    [Test]
    public void TestThreadChannelEmitUserMention()
    {
        var mentionedReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var thread = message.CreateThread();
            thread.Join();
            chat.StartListeningForMentionEvents(user.Id);
            chat.OnMentionEvent += mentionEvent =>
            {
                Assert.True(mentionEvent.Payload.Contains("heyyy"));
                mentionedReset.Set();
            };
            thread.EmitUserMention(user.Id, "99999999999999999", "heyyy");
        };
        channel.SendText("thread_start_message");
        var read = mentionedReset.WaitOne(10000);
        Assert.True(read);
    }
    
    [Test]
    public void TestThreadMessageParentChannelPinning()
    {
        var historyReadReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var thread = message.CreateThread();
            thread.Join();
            thread.SendText("one");
            thread.SendText("two");
            thread.SendText("three");
            
            await Task.Delay(3000);
            
            var history = thread.GetThreadHistory("99999999999999999", "00000000000000000", 3);
            var threadMessage = history[0];
            threadMessage.PinMessageToParentChannel();
            
            await Task.Delay(3000);

            Assert.True(channel.TryGetPinnedMessage(out var pinnedMessage) && pinnedMessage.MessageText == threadMessage.MessageText);
            threadMessage.UnPinMessageFromParentChannel();
            
            await Task.Delay(5000);

            Assert.False(channel.TryGetPinnedMessage(out _));
            historyReadReset.Set();
        };
        channel.SendText("thread_start_message");
        var read = historyReadReset.WaitOne(15000);
        Assert.True(read);
    }

    [Test]
    public void TestThreadMessageUpdate()
    {
        var messageUpdatedReset = new ManualResetEvent(false);
        channel.OnMessageReceived += async message =>
        {
            var thread = message.CreateThread();
            thread.Join();
            thread.SendText("one");
            thread.SendText("two");
            thread.SendText("three");
            
            await Task.Delay(3000);
            
            var history = thread.GetThreadHistory("99999999999999999", "00000000000000000", 3);
            var threadMessage = history[0];
            
            threadMessage.StartListeningForUpdates();
            threadMessage.OnThreadMessageUpdated += updatedThreadMessage =>
            {
                Assert.True(updatedThreadMessage.MessageText == "new_text");
                messageUpdatedReset.Set();
            };
            threadMessage.EditMessageText("new_text");
        };
        channel.SendText("thread_start_message");
        var updated = messageUpdatedReset.WaitOne(15000);
        Assert.True(updated);
    }
}