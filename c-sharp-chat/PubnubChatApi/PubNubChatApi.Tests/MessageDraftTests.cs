using PubNubChatAPI.Entities;
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
    public async Task Setup()
    {
        chat = await Chat.CreateInstance(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "message_draft_tests_user")
        );
        channel = await chat.CreatePublicConversation("message_draft_tests_channel", new ChatChannelData()
        {
            ChannelName = "MessageDraftTestingChannel"
        });
        if (!chat.OLD_TryGetCurrentUser(out var user))
        {
            Assert.Fail();
        }

        channel.Join();
        await Task.Delay(3000);
        
        if (!chat.OLD_TryGetUser("mock_user", out dummyUser))
        {
            dummyUser = await chat.OLD_CreateUser("mock_user", new ChatUserData()
            {
                Username = "Mock Usernamiski"
            });
        }

        if (!chat.TryGetChannel("dummy_channel", out dummyChannel))
        {
            dummyChannel = await chat.CreatePublicConversation("dummy_channel");
        }
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
    public async Task TestInsertSuggestedMention()
    {
        var messageDraft = channel.CreateMessageDraft();
        messageDraft.SetSearchForSuggestions(true);
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
                //TODO: to be re-enabled after Channel and Link mentions approach unification
                /*case "channel_suggestion":
                    var channelSuggestion =
                        mentions.FirstOrDefault(x => x.Target is { Target: "message_draft_tests_channel", Type: MentionType.Channel });
                
                    Assert.True(elements.Any(x => x.Text.Contains("now mention #MessageDraft")) && channelSuggestion != null,
                        "Received incorrect channel suggestion");
                    step = "channel_inserted";
                
                    messageDraft.InsertSuggestedMention(channelSuggestion, channelSuggestion.ReplaceTo);
                    break;
                case "channel_inserted":
                    Assert.True(elements.Any(x => x.Text.Contains("MessageDraftTestingChannel")));
                    successReset.Set();
                    break;*/
                default:
                    Assert.Fail("Unexpected draft update callback flow in test");
                    break;
            }
        };
        messageDraft.InsertText(0, "maybe i'll mention @Mock");
        var userInserted = successReset.WaitOne(5000);
        Assert.True(userInserted, "didn't receive user insertion callback");

        //TODO: to be re-enabled after Channel and Link mentions approach unification
        /*step = "channel_suggestion";
        successReset = new ManualResetEvent(false);
        messageDraft.InsertText(0, "now mention #MessageDraft ");
        var channelInserted = successReset.WaitOne(5000);
        Assert.True(channelInserted, "didn't receive channel insertion callback");

        var messageReset = new ManualResetEvent(false);
        channel.OnMessageReceived += message =>
        {
            Assert.True(message.ReferencedChannels.Any(x => x.Id == channel.Id), "received message doesn't contain expected referenced channel");
            Assert.True(message.MentionedUsers.Any(x => x.Id == dummyUser.Id), "received message doesn't contain expected mentioned user");
            messageReset.Set();
        };
        await messageDraft.Send();
        var receivedMessage = messageReset.WaitOne(10000);
        Assert.True(receivedMessage, "didn't receive message callback");*/
    }

    //TODO: to be re-enabled after Channel and Link mentions approach unification
    /*[Test]
    public async Task TestAddAndSendTextLink()
    {
        var messageDraft = channel.CreateMessageDraft();
        messageDraft.InsertText(0, "some text goes here");
        var updateReset = new ManualResetEvent(false);
        messageDraft.OnDraftUpdated += (elements, mentions) =>
        {
            Assert.True(elements.Any(x => x is { Text: "some", MentionTarget: {Target: "www.pubnub.com", Type: MentionType.Url} })
                , "updated message draft doesn't contain expected element");
            updateReset.Set();
        };
        messageDraft.AddMention(0, 4, new MentionTarget()
        {
            Target = "www.pubnub.com",
            Type = MentionType.Url
        });
        var updated = updateReset.WaitOne(3000);
        Assert.True(updated, "didn't receive md update callback");
        
        var messageReset = new ManualResetEvent(false);
        channel.OnMessageReceived += message =>
        {
            Assert.True(message.TextLinks.Any(x => x.Link == "www.pubnub.com"), "didn't find expected link in received message");
            messageReset.Set();
        };
        await messageDraft.Send();
        var received = messageReset.WaitOne(6000);
        Assert.True(received, "didn't receive message callback after md send");
    }*/

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