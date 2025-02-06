using System.Diagnostics;
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
        chat = await Chat.CreateInstance(new PubnubChatConfig(
            PubnubTestsParameters.PublishKey,
            PubnubTestsParameters.SubscribeKey,
            "restrictions_tests_user")
        );
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
        var channel = await chat.CreatePublicConversation("new_channel");

        await Task.Delay(2000);

        var restriction = new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "Some Reason"
        };
        await channel.SetRestrictions(user.Id, restriction);

        await Task.Delay(3000);

        var fetchedRestriction = await channel.GetUserRestrictions(user);

        Assert.True(restriction.Ban == fetchedRestriction.Ban && restriction.Mute == fetchedRestriction.Mute &&
                    restriction.Reason == fetchedRestriction.Reason);

        var restrictionFromUser = await user.GetChannelRestrictions(channel);
        
        Assert.True(restriction.Ban == restrictionFromUser.Ban && restriction.Mute == restrictionFromUser.Mute &&
                    restriction.Reason == restrictionFromUser.Reason);
    }

    [Test]
    public async Task TestGetRestrictionsSets()
    {
        var user = await chat.GetOrCreateUser("user1234");
        var channel = await chat.CreatePublicConversation("new_channel_2");

        await Task.Delay(4000);

        var restriction = new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "Some Reason"
        };
        await channel.SetRestrictions(user.Id, restriction);

        await Task.Delay(4000);

        var a = await channel.GetUsersRestrictions();
        var b = await user.GetChannelsRestrictions();
        
        Assert.True(a.Restrictions.Any(x => x.UserId == user.Id));
        Assert.True(b.Restrictions.Any(x => x.ChannelId == channel.Id));
    }
}