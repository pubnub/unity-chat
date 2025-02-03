/*using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

[TestFixture]
public class MessageDraftTests
{
    private Chat chat;
    private Channel channel;
    private User dummyUser;
    private Channel dummyChannel;

    [SetUp]
    public void Setup()
    {
        chat = new Chat(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "message_draft_tests_user")
        );
        channel = chat.CreatePublicConversation("message_draft_tests_channel");
        if (!chat.TryGetCurrentUser(out var user))
        {
            Assert.Fail();
        }

        channel.Join();
        if (!chat.TryGetUser("mock_user", out dummyUser))
        {
            dummyUser = chat.CreateUser("mock_user", new ChatUserData()
            {
                Username = "Mock Usernamiski"
            });
        }

        if (!chat.TryGetChannel("dummy_channel", out dummyChannel))
        {
            dummyChannel = chat.CreatePublicConversation("dummy_channel");
        }
    }

    [Test]
    public void TestInsertAndRemoveText()
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
    public void TestInsertSuggestedMention()
    {
        var messageDraft = channel.CreateMessageDraft();
        messageDraft.SetSearchForSuggestions(true);
        var successReset = new ManualResetEvent(false);
        var hadSuggestion = false;
        messageDraft.OnDraftUpdated += (elements, mentions) =>
        {
            if (!hadSuggestion)
            {
                var userSuggestion =
                    mentions.FirstOrDefault(x => x.Target is { Target: "mock_user", Type: MentionType.User });
                
                Assert.True(elements.Any(x => x.Text == "maybe i'll mention @Mock") && userSuggestion != null,
                    "Received correct user suggestion");
                hadSuggestion = true;
                
                messageDraft.InsertSuggestedMention(userSuggestion, userSuggestion.ReplaceTo);
            }
            else
            {
                Assert.True(elements.Any(x => x.Text.Contains("Mock Usernamiski")));
                successReset.Set();
            }
        };
        messageDraft.InsertText(0, "maybe i'll mention @Mock");
        var gotCallback = successReset.WaitOne(5000);
        Assert.True(gotCallback);
    }

    [Test]
    public void TestAddAndRemoveMention()
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
    public void TestUpdate()
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
    public void TestSend()
    {
        var successReset = new ManualResetEvent(false);
        channel.OnMessageReceived += message =>
        {
            Assert.True(message.MessageText == "draft_text");
            successReset.Set();
        };
        var messageDraft = channel.CreateMessageDraft();
        messageDraft.InsertText(0, "draft_text");
        messageDraft.Send();
        var gotCallback = successReset.WaitOne(6000);
        Assert.True(gotCallback);
    }
}*/