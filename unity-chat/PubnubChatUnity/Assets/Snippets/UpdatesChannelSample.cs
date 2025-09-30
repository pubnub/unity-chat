// snippet.using
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using Channel = PubnubChatApi.Channel;

// snippet.end

public class UpdatesChannelSample
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
    
    public static async Task UpdateChannelUsingChannelObjectExample()
    {
        // snippet.update_channel_using_channel_object_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;
        
        var updatedChannelData = new ChatChannelData
        {
            Description = "Channel for CRM tickets"
        };

        await channel.Update(updatedChannelData);
        // snippet.end
    }
    
    public static async Task UpdateChannelUsingChatObjectExample()
    {
        // snippet.update_channel_using_chat_object_example
        var updatedChannelData = new ChatChannelData
        {
            Description = "Channel for CRM tickets"
        };

        await chat.UpdateChannel("support", updatedChannelData);
        // snippet.end
    }
    
    public static async Task ListenForChannelUpdatesExample()
    {
        // snippet.listen_for_channel_updates_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error)
        {
            Console.WriteLine("Couldn't find channel!");
            return;
        }
        var channel = channelResult.Result;
        channel.SetListeningForUpdates(true);
        channel.OnChannelUpdate += OnChannelUpdateHandler; // or use lambda

        void OnChannelUpdateHandler(Channel channel)
        {
            Console.WriteLine($"Channel updated: {channel.Id}");
        }
        // snippet.end
    }
    
    public static async Task AddListenerToChannelsUpdateExample()
    {
        // snippet.add_listener_to_channels_update_example
        List<string> channelIds = new List<string> { "support", "incidentManagement" };
        Action<Channel> listener = (Channel channel) => 
            {
                // Print the updated channel name
                Console.WriteLine("Updated Channel Name: " + channel.Name);
            };

        await chat.AddListenerToChannelsUpdate(channelIds, listener);
        // snippet.end
    }
}
