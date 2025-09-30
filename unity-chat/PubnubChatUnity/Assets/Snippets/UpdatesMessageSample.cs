// snippet.using
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class UpdatesMessageSample
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
    
    public static async Task EditMessageTextExample(Message message)
    {
        // snippet.edit_message_text_example
        await message.EditMessageText("Your ticket number is 78398");
        // snippet.end
    }
    
    public static async Task SetListeningForUpdatesExample()
    {
        // snippet.set_listening_for_updates_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Debug.Log("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;
        // get first message from history
        var messageResult = await channel.GetMessageHistory(null, null, 1);
        if (messageResult.Error || !messageResult.Result.Any())
        {
            Debug.Log("Couldn't find message!");
            return;
        }
        var message = messageResult.Result.First();
        message.SetListeningForUpdates(true);
        message.OnMessageUpdated += OnMessageUpdatedHandler; // or use lambda

        void OnMessageUpdatedHandler(Message message)
        {
            Debug.Log($"Message updated");
        }
        // snippet.end
    }
    
    public static async Task AddListenerToMessagesUpdateExample()
    {
        // snippet.add_listener_to_messages_update_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Debug.Log("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;

        var messagesResult = await channel.GetMessageHistory("15343325214676133", null, 10);
        if (messagesResult.Error)
        {
            Debug.Log("Couldn't get message history!");
            return;
        }
        var messages = messagesResult.Result;

        List<string> timetokens = new List<string>();
        
        // get the timetokens
        foreach (var message in messages)
        {
            // Get the time token of the current message
            string timeToken = message.TimeToken;
            
            // Add the time token to the list
            timetokens.Add(timeToken);
        }
        
        void OnMessageUpdatedHandler(Message message)
        {
            Debug.Log($"Message updated");
        }

        chat.AddListenerToMessagesUpdate(channelId: "support", messageTimeTokens: timetokens, listener: OnMessageUpdatedHandler);
        // snippet.end
    }
}
