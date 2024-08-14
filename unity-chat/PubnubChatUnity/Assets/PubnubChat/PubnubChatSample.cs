using System.Threading.Tasks;
using PubnubChat;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;
using UnityEngine;

/// <summary>
/// Contains a simple example of creating and using the Pubnub Chat API for Unity
/// </summary>
public class PubnubChatSample : MonoBehaviour
{
    [SerializeField] private PubnubChatConfigAsset configAsset;
    
    private async void Start()
    {
        //Initialize Chat instance with Pubnub keys + user ID
        var chat = new Chat(configAsset);
        //Create config-defined user id handle
        var user = chat.CreateUser(configAsset.UserId);
        
        //Create a new channel
        var channel = chat.CreatePublicConversation("MainChannel");
        //Define reaction on receiving new messages
        channel.OnMessageReceived += message => Debug.Log($"Received message: {message.MessageText}");
        //Join channel
        channel.Join();
        //Send test message
        channel.SendText("Hello World from Pubnub!");
        
        //React on user data being updated
        user.OnUserUpdated += updatedUser =>
            Debug.Log($"{updatedUser.Id} has been updated! Their name is now {updatedUser.UserName}");
        //Update our user data
        user.Update(new ChatUserData()
        {
            Username = "FancyUserName"
        });
        
        //Send a few more messages
        channel.SendText("Hi!");
        channel.SendText("Hello!");
        channel.SendText("Anyone there?");
        channel.SendText("Me!");

        //Wait a moment to wait for them to be processed
        await Task.Delay(15000);
        
        //Fetch message history (from all time)
        foreach (var historyMessage in channel.GetMessageHistory("99999999999999999", "00000000000000000", 50))
        {
            Debug.Log($"Message from history with timetoken {historyMessage.TimeToken}: {historyMessage.MessageText}");
        }
        
        //Get main users memberships
        var userMembershipsWrapper = user.GetMemberships();
        foreach (var userMembership in userMembershipsWrapper.Memberships)
        {
            Debug.Log($"Membership - User: {userMembership.UserId}, Channel: {userMembership.ChannelId}");
        }
        
        //Set a restriction on user
        user.SetRestriction(channel.Id, new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "You were mean!"
        });
       
        //Wait a moment to wait for the restriction to be registered
        await Task.Delay(15000);
        
        //Print channel's user restriction
        var restriction = channel.GetUserRestrictions(user);
        Debug.Log($"{user.Id}'s ban status is: {restriction.Ban}, reason: {restriction.Reason}");
    }
}