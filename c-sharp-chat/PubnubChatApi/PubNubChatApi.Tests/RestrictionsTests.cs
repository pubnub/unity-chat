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
        chat = TestUtils.AssertOperation(await Chat.CreateInstance(new PubnubChatConfig(storeUserActivityTimestamp: true), new PNConfiguration(new UserId("restrictions_tests_user"))
        {
            PublishKey = PubnubTestsParameters.PublishKey,
            SubscribeKey = PubnubTestsParameters.SubscribeKey
        }));
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
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("new_channel"));
        
        await Task.Delay(2000);

        var restriction = new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "Some Reason"
        };
        TestUtils.AssertOperation(await channel.SetRestrictions(user.Id, restriction));

        await Task.Delay(3000);
        
        var fetchedRestriction = TestUtils.AssertOperation(await channel.GetUserRestrictions(user));

        Assert.True(restriction.Ban == fetchedRestriction.Ban && restriction.Mute == fetchedRestriction.Mute &&
                    restriction.Reason == fetchedRestriction.Reason);

        var restrictionFromUser = TestUtils.AssertOperation(await user.GetChannelRestrictions(channel));
        
        Assert.True(restriction.Ban == restrictionFromUser.Ban && restriction.Mute == restrictionFromUser.Mute &&
                    restriction.Reason == restrictionFromUser.Reason);
    }

    [Test]
    public async Task TestGetRestrictionsSets()
    {
        var user = await chat.GetOrCreateUser("user1234");
        var channel = TestUtils.AssertOperation(await chat.CreatePublicConversation("new_channel"));

        await Task.Delay(4000);

        var restriction = new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "Some Reason"
        };
        TestUtils.AssertOperation(await channel.SetRestrictions(user.Id, restriction));
        
        await Task.Delay(4000);

        var a = TestUtils.AssertOperation(await channel.GetUsersRestrictions());
        var b = TestUtils.AssertOperation(await user.GetChannelsRestrictions());
        
        Assert.True(a.Restrictions.Any(x => x.UserId == user.Id));
        Assert.True(b.Restrictions.Any(x => x.ChannelId == channel.Id));
    }
}