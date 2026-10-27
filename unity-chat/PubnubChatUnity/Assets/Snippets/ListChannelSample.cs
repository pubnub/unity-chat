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
        var getChannels = await chat.GetChannels();

        if (getChannels.Error)
        {
            Debug.LogError($"An error has occured when trying to fetch channels: {getChannels.Exception.Message}");
            return;
        }

        // print all channel IDs
        foreach (var channel in getChannels.Result.Channels)
        {
            Debug.Log(channel.Id);
        }
        // snippet.end
    }
    
    public static async Task PaginationExample()
    {
        // snippet.pagination_example
        // fetch the initial 25 channels
        var getChannels = await chat.GetChannels(limit: 25);
        
        if (getChannels.Error)
        {
            Debug.LogError($"An error has occured when trying to fetch channels: {getChannels.Exception.Message}");
            return;
        }

        var channelsWrapper = getChannels.Result;

        Debug.Log("Initial 25 channels:");
        foreach (var channel in channelsWrapper.Channels)
        {
            Debug.Log($"Id: {channel.Id}");
        }

        // fetch the next set of channels using the page object from returned wrapper
        var nextGetChannels = await chat.GetChannels(limit: 25, page: channelsWrapper.Page);

        if (nextGetChannels.Error)
        {
            Debug.LogError($"An error has occured when trying to fetch next channels: {getChannels.Exception.Message}");
            return;
        }
        
        Debug.Log("\nNext set of channels:");
        foreach (var channel in nextGetChannels.Result.Channels)
        {
            Debug.Log($"Id: {channel.Id}");
        }
        // snippet.end
    }
}
