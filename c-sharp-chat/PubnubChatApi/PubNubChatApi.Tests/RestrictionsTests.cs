using PubnubApi;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubNubChatApi.Tests;

[TestFixture]
public class RestrictionsTests
{
    private Chat chat;

    [SetUp]
    public async Task Setup()
    {
        var createChat = await Chat.CreateInstance(new PubnubChatConfig(storeUserActivityTimestamp: true), new PNConfiguration(new UserId("restrictions_tests_user"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        });
        if (createChat.Error)
        {
            Assert.Fail($"Failed to create chat! Error: {createChat.Exception.Message}");
        }
        chat = createChat.Result;
    }
    
    [TearDown]
    public async Task CleanUp()
    {
        chat.Destroy();
        await Task.Delay(3000);
    }

    [Test]
    public async Task TestSetRestrictions()
    {
        var user = await chat.GetOrCreateUser("user123");
        var createChannel = await chat.CreatePublicConversation("new_channel");
        if (createChannel.Error)
        {
            Assert.Fail($"Failed to create channel, error: {createChannel.Exception.Message}");
        }
        var channel = createChannel.Result;
        
        await Task.Delay(2000);

        var restriction = new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "Some Reason"
        };
        var setRestrictions = await channel.SetRestrictions(user.Id, restriction);
        if (setRestrictions.Error)
        {
            Assert.Fail($"Failed to set restrictions, error: {setRestrictions.Exception.Message}");
        }

        await Task.Delay(3000);

        var getUser = await channel.GetUserRestrictions(user);
        if (getUser.Error)
        {
            Assert.Fail($"Failed to fetch User restrictions. Exception: {getUser.Exception}");
        }
        var fetchedRestriction = getUser.Result;

        Assert.True(restriction.Ban == fetchedRestriction.Ban && restriction.Mute == fetchedRestriction.Mute &&
                    restriction.Reason == fetchedRestriction.Reason);

        var getChannel = await user.GetChannelRestrictions(channel);
        if (getChannel.Error)
        {
            Assert.Fail($"Failed to fetch Channel restrictions. Exception: {getChannel.Exception}");
        }
        var restrictionFromUser = getChannel.Result;
        
        Assert.True(restriction.Ban == restrictionFromUser.Ban && restriction.Mute == restrictionFromUser.Mute &&
                    restriction.Reason == restrictionFromUser.Reason);
    }

    [Test]
    public async Task TestGetRestrictionsSets()
    {
        var user = await chat.GetOrCreateUser("user1234");
        var createChannel = await chat.CreatePublicConversation("new_channel");
        if (createChannel.Error)
        {
            Assert.Fail($"Failed to create channel, error: {createChannel.Exception.Message}");
        }
        var channel = createChannel.Result;

        await Task.Delay(4000);

        var restriction = new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "Some Reason"
        };
        var setRestrictions = await channel.SetRestrictions(user.Id, restriction);
        if (setRestrictions.Error)
        {
            Assert.Fail($"Failed to set restrictions, error: {setRestrictions.Exception.Message}");
        }
        
        await Task.Delay(4000);

        var a = await channel.GetUsersRestrictions();
        if (a.Error)
        {
            Assert.Fail($"Failed to fetch Users restrictions. Exception: {a.Exception}");
        }
        var b = await user.GetChannelsRestrictions();
        if (b.Error)
        {
            Assert.Fail($"Failed to fetch Channels restrictions. Exception: {b.Exception}");
        }
        
        Assert.True(a.Result.Restrictions.Any(x => x.UserId == user.Id));
        Assert.True(b.Result.Restrictions.Any(x => x.ChannelId == channel.Id));
    }
}