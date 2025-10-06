// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class ListChannelSample
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
    
    public static async Task GetAllChannelsExample()
    {
        // snippet.get_all_channels_example
        // fetch all channels
        var channelsWrapper = await chat.GetChannels();

        // print all channel IDs
        foreach (var channel in channelsWrapper.Channels)
        {
            Debug.Log(channel.Id);
        }
        // snippet.end
    }
    
    public static async Task PaginationExample()
    {
        // snippet.pagination_example
        // fetch the initial 25 channels
        var channelsWrapper = await chat.GetChannels(limit: 25);

        Debug.Log("Initial 25 channels:");
        foreach (var channel in channelsWrapper.Channels)
        {
            Debug.Log($"Id: {channel.Id}");
        }

        // fetch the next set of channels using the page object from returned wrapper
        var nextChannelsWrapper = await chat.GetChannels(limit: 25, page: channelsWrapper.Page);

        Debug.Log("\nNext set of channels:");
        foreach (var channel in nextChannelsWrapper.Channels)
        {
            Debug.Log($"Id: {channel.Id}");
        }
        // snippet.end
    }
}
