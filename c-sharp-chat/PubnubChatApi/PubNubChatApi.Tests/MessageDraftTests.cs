using PubnubApi;
using PubnubChatApi;
using Channel = PubnubChatApi.Channel;

namespace PubNubChatApi.Tests;

[TestFixture]
public class MessageDraftTests
{
    private Chat chat;
    private Channel channel;
    private User dummyUser;

    [SetUp]
    public async Task Setup()
    {
        chat = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(storeUserActivityTimestamp: true), new PNConfiguration(new UserId("message_draft_tests_user"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        }));
        channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("message_draft_tests_channel", new ChatChannelData()
        {
            Name = "MessageDraftTestingChannel"
        }));
        await channel.Join();
        await Task.Delay(3000);

        dummyUser = await chat.GetOrCreateUser("mock_user", new ChatUserData()
        {
            Username = "Mock Usernamiski"
        });
    }

    [Test]
    public async Task TestInsertAndRemoveText()
    {
        var messageDraft = channel.CreateMessageDraft();
        var successReset = new ManualResetEvent(false);

        void InsertDelegateCallback(List<MessageElement> elements, List<SuggestedMention> mentions)
        {
            Assert.True(elements.Any(x => x.Text == "test insert"));
            successReset.Set();
        }

        messageDraft.OnDraftUpdated += InsertDelegateCallback;
        messageDraft.InsertText(0, "test insert");
        var gotCallback = successReset.WaitOne(5000);
        Assert.True(gotCallback, "Text insert success");

        successReset = new ManualResetEvent(false);
        messageDraft.OnDraftUpdated -= InsertDelegateCallback;
        messageDraft.OnDraftUpdated += (elements, mentions) =>
        {
            Assert.True(elements.Any(x => x.Text == "insert"));
            successReset.Set();
        };
        messageDraft.RemoveText(0, 5);
        gotCallback = successReset.WaitOne(5000);
        Assert.True(gotCallback, "Text remove success");
    }

    [Test]
    public async Task TestInsertSuggestedMentionsAndSend()
    {
        var messageDraft = channel.CreateMessageDraft(shouldSearchForSuggestions:true);
        var successReset = new ManualResetEvent(false);
        var step = "user_suggestion";
        messageDraft.OnDraftUpdated += (elements, mentions) =>
        {
            switch (step)
            {
                case "user_suggestion":
                    var userSuggestion =
                        mentions.FirstOrDefault(x => x.Target is { Target: "mock_user", Type: MentionType.User });
                
                    Assert.True(elements.Any(x => x.Text == "maybe i'll mention @Mock") && userSuggestion != null,
                        "Received incorrect user suggestion");
                    step = "user_inserted";
                
                    messageDraft.InsertSuggestedMention(userSuggestion, userSuggestion.ReplaceTo);
                    break;
                case "user_inserted":
                    Assert.True(elements.Any(x => x.Text.Contains("Mock Usernamiski")));
                    successReset.Set();
                    break;
                case "channel_suggestion":
                    var channelSuggestion =
                        mentions.FirstOrDefault(x => x.Target is { Target: "message_draft_tests_channel", Type: MentionType.Channel });
                
                    Assert.True(elements.Any(x => x.Text.Contains("now mention #MessageDraft")) && channelSuggestion != null,
                        "Received incorrect channel suggestion");
                    step = "channel_inserted";
                
                    messageDraft.InsertSuggestedMention(channelSuggestion, channelSuggestion.ReplaceTo);
                    break;
                case "channel_inserted":
                    Assert.True(elements.Any(x => x.Text.Contains("MessageDraftTestingChannel")), "channel wasn't inserted into MD");
                    successReset.Set();
                    break;
                case "link_inserted":
                    Assert.True(elements.Any(x => x.MentionTarget is {Type:MentionType.Url, Target:"www.pubnub.com"}), "text link wasn't insterted into MD");
                    successReset.Set();
                    break;
                default:
                    Assert.Fail("Unexpected draft update callback flow in test");
                    break;
            }
        };
        messageDraft.InsertText(0, "maybe i'll mention @Mock");
        var userInserted = successReset.WaitOne(5000);
        Assert.True(userInserted, "didn't receive user insertion callback");
        
        step = "channel_suggestion";
        successReset = new ManualResetEvent(false);
        messageDraft.InsertText(0, "now mention #MessageDraft ");
        var channelInserted = successReset.WaitOne(5000);
        Assert.True(channelInserted, "didn't receive channel insertion callback");

        step = "link_inserted";
        successReset = new ManualResetEvent(false);
        messageDraft.AddMention(0, 3, new MentionTarget(){Target = "www.pubnub.com", Type = MentionType.Url});
        var linkAdded = successReset.WaitOne(5000);
        Assert.True(channelInserted, "didn't receive text link insertion callback");

        var messageReset = new ManualResetEvent(false);
        Message messageFromDraft = null;
        channel.OnMessageReceived += message =>
        {
            messageFromDraft = message;
            messageReset.Set();
        };
        await messageDraft.Send();
        var receivedMessage = messageReset.WaitOne(10000);
        Assert.True(receivedMessage, "didn't receive message callback");
        if (messageFromDraft != null)
        {
            Assert.True(messageFromDraft.TextLinks.Any(x => x.Link == "www.pubnub.com"), "received message doesn't contain expected text link");
            Assert.True(messageFromDraft.ReferencedChannels.Any(x => x.Id == channel.Id), "received message doesn't contain expected referenced channel");
            Assert.True(messageFromDraft.MentionedUsers.Any(x => x.Id == dummyUser.Id), "received message doesn't contain expected mentioned user");
        }
    }

    [Test]
    public async Task TestAddAndRemoveMention()
    {
        var messageDraft = channel.CreateMessageDraft();
        var successReset = new ManualResetEvent(false);
        
        messageDraft.InsertText(0, "wololo and stuff");

        void AddMentionCallback(List<MessageElement> elements, List<SuggestedMention> mentions)
        {
            Assert.True(elements.Any(x => x.MentionTarget is {Target: "mock_user", Type: MentionType.User}));
            successReset.Set();
        }
        messageDraft.OnDraftUpdated += AddMentionCallback;
        
        messageDraft.AddMention(0, 6, new MentionTarget()
        {
            Target = "mock_user",
            Type = MentionType.User
        });
        var gotCallback = successReset.WaitOne(5000);
        Assert.True(gotCallback, "Add mention success");

        successReset = new ManualResetEvent(false);
        messageDraft.OnDraftUpdated -= AddMentionCallback;
        messageDraft.OnDraftUpdated += (elements, mentions) =>
        {
            Assert.False(elements.Any(x => x.MentionTarget is {Target: "mock_user", Type: MentionType.User}));
            successReset.Set();
        };
        messageDraft.RemoveMention(0);
        gotCallback = successReset.WaitOne(5000);
        Assert.True(gotCallback, "Remove mention success");
    }

    [Test]
    public async Task TestGetMessageElementsFromMessage()
    {
        var messageDraft = channel.CreateMessageDraft();

        messageDraft.InsertText(0, "Hello JohnDoe check general and click here for more");
        messageDraft.AddMention(6, 7, new MentionTarget { Type = MentionType.User, Target = "mock_user" });
        messageDraft.AddMention(20, 7, new MentionTarget { Type = MentionType.Channel, Target = "message_draft_tests_channel" });
        messageDraft.AddMention(32, 10, new MentionTarget { Type = MentionType.Url, Target = "https://www.pubnub.com" });

        var draftElements = messageDraft.GetMessageElements();

        var messageReset = new ManualResetEvent(false);
        List<MessageElement> receivedElements = null;
        channel.OnMessageReceived += message =>
        {
            receivedElements = message.GetMessageElements();
            messageReset.Set();
        };

        await messageDraft.Send();
        var receivedMessage = messageReset.WaitOne(10000);
        Assert.True(receivedMessage, "didn't receive message callback");

        Assert.NotNull(receivedElements);
        Assert.AreEqual(draftElements.Count, receivedElements.Count, "element count mismatch");
        for (int i = 0; i < draftElements.Count; i++)
        {
            Assert.AreEqual(draftElements[i].Text, receivedElements[i].Text, $"text mismatch at index {i}");
            if (draftElements[i].MentionTarget == null)
            {
                Assert.IsNull(receivedElements[i].MentionTarget, $"expected null MentionTarget at index {i}");
            }
            else
            {
                Assert.NotNull(receivedElements[i].MentionTarget, $"expected non-null MentionTarget at index {i}");
                Assert.AreEqual(draftElements[i].MentionTarget.Type, receivedElements[i].MentionTarget.Type, $"MentionTarget type mismatch at index {i}");
                Assert.AreEqual(draftElements[i].MentionTarget.Target, receivedElements[i].MentionTarget.Target, $"MentionTarget target mismatch at index {i}");
            }
        }
    }

    [Test]
    public async Task TestAppendText()
    {
        var messageDraft = channel.CreateMessageDraft();
        messageDraft.InsertText(0, "some text goes here");
        messageDraft.AppendText(", and some more goes at the end");
        var text = messageDraft.Text;
        Assert.True(text == "some text goes here, and some more goes at the end", "Wrong text in MD after AppendText()");
    }

    [Test]
    public async Task TestUpdate()
    {
        var messageDraft = channel.CreateMessageDraft();
        var successReset = new ManualResetEvent(false);
        messageDraft.OnDraftUpdated += (elements, mentions) =>
        {
            Assert.True(elements.Any(x => x.Text == "some text wololo"));
            successReset.Set();
        };
        messageDraft.Update("some text wololo");
        var gotCallback = successReset.WaitOne(5000);
        Assert.True(gotCallback);
    }


    [Test]
    public async Task TestSend()
    {
        var successReset = new ManualResetEvent(false);
        channel.OnMessageReceived += message =>
        {
            Assert.True(message.MessageText == "draft_text");
            successReset.Set();
        };
        var messageDraft = channel.CreateMessageDraft();
        messageDraft.InsertText(0, "draft_text");
        await messageDraft.Send();
        var gotCallback = successReset.WaitOne(6000);
        Assert.True(gotCallback);
    }
}