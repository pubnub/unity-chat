// snippet.using
using System;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class AccessControlSample
{
    private static Chat chat;

    static async Task Init()
    {
        // snippet.init
        // Configuration
        PubnubChatConfig chatConfig = new PubnubChatConfig();
        
        PNConfiguration pnConfiguration = new PNConfiguration(new UserId("myUniqueUserId"))
        {
            SubscribeKey = "demo",
            PublishKey = "demo",
            Secure = true
        };

        // Initialize Unity Chat
        var chatResult = await UnityChat.CreateInstance(chatConfig, pnConfiguration);
        if (!chatResult.Error)
        {
            chat = chatResult.Result;
        }
        // snippet.end
    }
    
    public static async Task CheckPermissionsExample()
    {
        // snippet.check_permissions_example
        var pnConfiguration = new PNConfiguration(new UserId("UserId"))
        {
            PublishKey = "PublishKey",
            SubscribeKey = "SubscribeKey",
            AuthKey = "AuthKey"
        };

        var chatConfig = new PubnubChatConfig();
        var chatResult = await Chat.CreateInstance(chatConfig, pnConfiguration);
        if (chatResult.Error)
        {
            Console.WriteLine("Failed to create chat instance");
            return;
        }
        var chat = chatResult.Result;

        // get the ChatAccessManager instance
        var chatAccessManager = chat.ChatAccessManager;

        // define the permissions, resource type, and resource name
        PubnubAccessPermission permissionToCheck = PubnubAccessPermission.Write;
        PubnubAccessResourceType resourceTypeToCheck = PubnubAccessResourceType.Channels;
        string channelName = "support";

        // check if the current user can send (write) messages to the 'support' channel
        bool canSendMessage = await chatAccessManager.CanI(permissionToCheck, resourceTypeToCheck, channelName);

        // output the result
        if (canSendMessage)
        {
            Console.WriteLine("The current user has permission to send messages to the 'support' channel.");
        }
        else
        {
            Console.WriteLine("The current user does not have permission to send messages to the 'support' channel.");
        }
        // snippet.end
    }
    
    public static async Task SetAuthTokenExample()
    {
        // snippet.set_auth_token_example
        var pnConfiguration = new PNConfiguration(new UserId("UserId"))
        {
            PublishKey = "PublishKey",
            SubscribeKey = "SubscribeKey"
        };
        var chatConfig = new PubnubChatConfig();
        var chatResult = await Chat.CreateInstance(chatConfig, pnConfiguration);
        if (chatResult.Error)
        {
            Console.WriteLine("Failed to create chat instance");
            return;
        }
        var chat = chatResult.Result;

        // Set a new authentication token
        chat.PubnubInstance.SetAuthToken("p0thisAkFl043rhDdHRsCkNyZXisRGNoYW6hanNlY3JldAFDZ3Jwsample3KgQ3NwY6BDcGF0pERjaGFuoENnctokenVzcqBDc3BjoERtZXRhoENzaWdYIGOAeTyWGJI");
        // snippet.end
    }
    
    public static async Task ParseTokenExample()
    {
        // snippet.parse_token_example
        var pnConfiguration = new PNConfiguration(new UserId("UserId"))
        {
            PublishKey = "PublishKey",
            SubscribeKey = "SubscribeKey"
        };
        var chatConfig = new PubnubChatConfig();
        var chatResult = await Chat.CreateInstance(chatConfig, pnConfiguration);
        if (chatResult.Error)
        {
            Console.WriteLine("Failed to create chat instance");
            return;
        }
        var chat = chatResult.Result;

        // Parse an existing token
        var tokenDetails = chat.PubnubInstance.ParseToken("p0thisAkFl043rhDdHRsCkNyZXisRGNoYW6hanNlY3JldAFDZ3Jwsample3KgQ3NwY6BDcGF0pERjaGFuoENnctokenVzcqBDc3BjoERtZXRhoENzaWdYIGOAeTyWGJI");

        // Output the token details
        Console.WriteLine("Token Details: " + chat.PubnubInstance.JsonPluggableLibrary.SerializeToJsonString(tokenDetails));
        // snippet.end
    }
}
