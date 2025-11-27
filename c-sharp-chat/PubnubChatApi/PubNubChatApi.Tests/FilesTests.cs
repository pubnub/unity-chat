using PubnubApi;
using PubnubChatApi;
using Channel = PubnubChatApi.Channel;

namespace PubNubChatApi.Tests;

[TestFixture]
public class FilesTests
{
    private Chat chat;
    private Channel channel;
    private User user;

    [SetUp]
    public async Task Setup()
    {
        chat = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(),
            new PNConfiguration(new UserId("file_tests_user"))
            {
                PublishKey = PubnubTestsParameters.PublishKey,
                SubscribeKey = PubnubTestsParameters.SubscribeKey
            }));
        channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("file_tests_channel"));
        user = TestUtils.AssertOperation(await chat.GetCurrentUser());
        await channel.Join();
        await Task.Delay(3500);
    }

    [TearDown]
    public async Task CleanUp()
    {
        await ClearChannelOfFiles();
        await channel.Leave();
        await Task.Delay(3000);
        chat.Destroy();
        await Task.Delay(3000);
    }

    private async Task ClearChannelOfFiles()
    {
        var files = await channel.GetFiles();
        if (files.Error)
        {
            Assert.Fail($"Error in files cleanup: {files.Exception.Message}");
            return;
        }

        foreach (var file in files.Result.Files)
        {
            var result = await channel.DeleteFile(file.Id, file.Name);
            if (result.Error)
            {
                Assert.Fail($"Error in files cleanup: {files.Exception.Message}");
            }
        }
    }

    [Test]
    public async Task TestFileUploadInMessage()
    {
        //Cleanup: delete files from channel
        await ClearChannelOfFiles();

        var receivedMessageReset = new ManualResetEvent(false);
        Message receivedMessage = null;
        channel.OnMessageReceived += message =>
        {
            if (message.MessageText == "FILE")
            {
                receivedMessage = message;
                receivedMessageReset.Set();
            }
        };

        //Add file to SendTextParams and send message
        TestUtils.AssertOperation(await channel.SendText("FILE", new SendTextParams()
        {
            Files =
            [
                new ChatInputFile()
                {
                    Name = "the_file",
                    Type = "text",
                    Source = "fileupload.txt"
                }
            ]
        }));

        //Receive message and check message.Files
        var received = receivedMessageReset.WaitOne(10000);
        Assert.True(received, "Did not receive message with file at all!");
        Assert.True(receivedMessage != null, "receivedMessage was null!");
        Assert.True(receivedMessage.Files != null, "receivedMessage.Files was null!");
        Assert.True(receivedMessage.Files.Count == 1,
            $"receivedMessage.Files.Count was {receivedMessage.Files.Count} instead of 1!");
        var receivedFile = receivedMessage.Files[0];
        Assert.True(receivedFile.Name == "the_file",
            $"Expected file name \"the_file\" but got \"{receivedFile.Name}\"");
        Assert.True(receivedFile.Type == "text", $"Expected file type \"text\" but got \"{receivedFile.Type}\"");
        Assert.True(!string.IsNullOrEmpty(receivedFile.Id), "File ID is empty");
        Assert.True(!string.IsNullOrEmpty(receivedFile.Url), "File URL is empty");

        //Check channel.GetFiles() for the file and check if data matches with the one from message.Files
        var channelFiles = TestUtils.AssertOperation(await channel.GetFiles());
        Assert.True(
            channelFiles.Files.Any(x =>
                x.Id == receivedFile.Id && x.Name == receivedFile.Name && x.Url == receivedFile.Url),
            "Did not find message file in channel.GetFiles()!");
    }

    [Test]
    public async Task TestFileUploadInMessageDraft()
    {
        //Cleanup: delete files from channel
        await ClearChannelOfFiles();

        var receivedMessageReset = new ManualResetEvent(false);
        Message receivedMessage = null;
        channel.OnMessageReceived += message =>
        {
            if (message.MessageText == "FILE")
            {
                receivedMessage = message;
                receivedMessageReset.Set();
            }
        };

        //Add file to SendTextParams in a MessageDraft and send message
        var messageDraft = channel.CreateMessageDraft();
        messageDraft.InsertText(0, "FILE");
        messageDraft.Files.Add(new ChatInputFile()
        {
            Name = "the_file",
            Type = "text",
            Source = "fileupload.txt"
        });
        
        TestUtils.AssertOperation(await messageDraft.Send());

        //Receive message and check message.Files
        var received = receivedMessageReset.WaitOne(10000);
        Assert.True(received, "Did not receive message with file at all!");
        Assert.True(receivedMessage != null, "receivedMessage was null!");
        Assert.True(receivedMessage.Files != null, "receivedMessage.Files was null!");
        Assert.True(receivedMessage.Files.Count == 1,
            $"receivedMessage.Files.Count was {receivedMessage.Files.Count} instead of 1!");
        var receivedFile = receivedMessage.Files[0];
        Assert.True(receivedFile.Name == "the_file",
            $"Expected file name \"the_file\" but got \"{receivedFile.Name}\"");
        Assert.True(receivedFile.Type == "text", $"Expected file type \"text\" but got \"{receivedFile.Type}\"");
        Assert.True(!string.IsNullOrEmpty(receivedFile.Id), "File ID is empty");
        Assert.True(!string.IsNullOrEmpty(receivedFile.Url), "File URL is empty");

        //Check channel.GetFiles() for the file and check if data matches with the one from message.Files
        var channelFiles = TestUtils.AssertOperation(await channel.GetFiles());
        Assert.True(
            channelFiles.Files.Any(x =>
                x.Id == receivedFile.Id && x.Name == receivedFile.Name && x.Url == receivedFile.Url),
            "Did not find message file in channel.GetFiles()!");
    }

    [Test]
    public async Task TestFileUploadErrorHandling()
    {
        var receivedMessageReset = new ManualResetEvent(false);
        channel.OnMessageReceived += message =>
        {
            if (message.MessageText == "FILE TOO BIG")
            {
                receivedMessageReset.Set();
            }
        };
        
        var sendResult = await channel.SendText("FILE TOO BIG", new SendTextParams()
        {
            Files =
            [
                new ChatInputFile()
                {
                    Name = "the_BIG_file",
                    Type = "image",
                    Source = "file_large.png"
                }
            ]
        });
        Assert.True(sendResult.Error, "sendResult.Error should be true for file over size limit");
        Assert.True(sendResult.Exception.Message.Contains("Your proposed upload exceeds the maximum allowed size"), "Error message should contain info about file size");

        var received = receivedMessageReset.WaitOne(5000);
        Assert.False(received, "SendText should abort and not send message in case of file upload error");
    }
}