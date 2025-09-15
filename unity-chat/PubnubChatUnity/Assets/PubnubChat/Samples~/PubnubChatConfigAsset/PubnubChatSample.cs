using System.Threading.Tasks;
using PubnubApi;
using PubnubChat;
using PubnubChat.Runtime;
using PubnubChatApi.Entities.Data;
using UnityEngine;

/// <summary>
/// Contains a simple example of creating and using the Pubnub Chat API for Unity
/// </summary>
public class PubnubChatSample : MonoBehaviour
{
    //Note that you can also use the PNConfigAsset scriptable object here.
    [SerializeField] private string publishKey;
    [SerializeField] private string subscribeKey;
    [SerializeField] private string userId;
    
    [SerializeField] private PubnubChatConfigAsset configAsset;
    
    private async void Start()
    {
        //Initialize Chat instance with Pubnub keys + user ID
        var createChat = await UnityChat.CreateInstance(configAsset, new PNConfiguration(new UserId(userId))
        {
            PublishKey = publishKey,
            SubscribeKey = subscribeKey,
            LogLevel = PubnubLogLevel.Error
        }, unityLogging: true);
        
        //Abort if initialization failed - because we set LogLevel to PubnubLogLevel.Error we will also see the exact reason in the Unity console.
        if (createChat.Error)
        {
            Debug.LogError($@"Chat initialization failed! Error: {createChat.Exception.Message}");
            return;
        }
        var chat = createChat.Result;

        //Get config-defined user id handle, abort on fail
        var getCurrentUser = await chat.GetCurrentUser();
        if (getCurrentUser.Error)
        {
            Debug.LogError($"Wasn't able to get current user! Is the Chat Config set-up correctly? Error: {getCurrentUser.Exception.Message}");
            return;
        }
        var user = getCurrentUser.Result;

        //Create a new channel, abort on fail
        var createChannel = await chat.CreatePublicConversation("MainChannel");
        if (createChannel.Error)
        {
            Debug.LogError($"Wasn't able to create channel! Error: {createChannel.Exception.Message}");
            return;
        }
        var channel = createChannel.Result;
        
        //Define reaction on receiving new messages
        channel.OnMessageReceived += message => Debug.Log($"Received message: {message.MessageText}");
        //Join channel + give time to establish connection
        //Note that Join(), like all methods that make contact with the server, also returns a ChatOperationResult
        //We could also potentially have abort logic here if the operation failed.
        await channel.Join();
        await Task.Delay(4000);
        
        //Send test message
        await channel.SendText("Hello World from Pubnub!");
        
        //React on user data being updated
        user.SetListeningForUpdates(true);
        await Task.Delay(2500);
        
        user.OnUserUpdated += updatedUser =>
            Debug.Log($"{updatedUser.Id} has been updated! Their name is now {updatedUser.UserName}");
        //Update our user data
        await user.Update(new ChatUserData()
        {
            Username = "FancyUserName"
        });
        
        //Send a few more messages
        await channel.SendText("Hi!");
        await channel.SendText("Hello!");
        await channel.SendText("Anyone there?");
        await channel.SendText("Me!");

        //Wait a moment to wait for them to be processed
        await Task.Delay(15000);
        
        //Fetch message history (from all time), again with abort logic
        var getHistory = await channel.GetMessageHistory("99999999999999999", "00000000000000000", 50);
        if (getHistory.Error)
        {
            Debug.LogError($"Wasn't able to get history! Error: {getHistory.Exception.Message}");
            return;
        }
        foreach (var historyMessage in getHistory.Result)
        {
            Debug.Log($"Message from history with timetoken {historyMessage.TimeToken}: {historyMessage.MessageText}");
        }
        
        //Get main users memberships, again with abort logic
        var getMemberships = await user.GetMemberships();
        if (getMemberships.Error)
        {
            Debug.LogError($"Wasn't able to get memberships! Error: {getMemberships.Exception.Message}");
            return;
        }
        foreach (var userMembership in getMemberships.Result.Memberships)
        {
            Debug.Log($"Membership - User: {userMembership.UserId}, Channel: {userMembership.ChannelId}");
        }
        
        //Set a restriction on user
        await user.SetRestriction(channel.Id, new Restriction()
        {
            Ban = true,
            Mute = true,
            Reason = "You were mean!"
        });
       
        //Wait a moment to wait for the restriction to be registered
        await Task.Delay(15000);
        
        //Print channel's user restriction, this time with no abort logic - shorter syntax but can result in exception if null result isn't handled.
        var restriction = (await channel.GetUserRestrictions(user)).Result;
        Debug.Log($"{user.Id}'s ban status is: {restriction.Ban}, reason: {restriction.Reason}");
    }
}
