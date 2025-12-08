// snippet.using
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class PushSamples
{
    private static Chat chat;

    static async Task Init()
    {
        // snippet.init
        // Chat configuration with FCM push settings (APNS2 is also supported)
        PubnubChatConfig chatConfig = new PubnubChatConfig()
        {
            PushNotifications =
            {
                SendPushes = true, 
                DeviceGateway = PNPushType.FCM, 
                DeviceToken = "some_device"
            }
        };
        
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

    public static async Task GetPushConfig()
    {
        // snippet.push_config
        var pushConfig1 = chat.Config.PushNotifications;
        //or:
        var pushConfig2 = chat.GetCommonPushOptions;
        // snippet.end
    }

    public static async Task RegisterPushChannel()
    {
        // snippet.channel_register_push
        var getChannel = await chat.GetChannel("some_channel");
        if (getChannel.Error)
        {
            Debug.LogError($"Could not get channel! Error: {getChannel.Exception.Message}");
            return;
        }
        var channel = getChannel.Result;

        var result = await channel.RegisterForPush();
        if (result.Error)
        {
            Debug.LogError($"Error when trying to register channel for push: {result.Exception.Message}");
        }
        
        //Alternatively you can also use:
        await chat.RegisterPushChannels(new List<string>() { channel.Id });
        // snippet.end
    }
    
    public static async Task UnRegisterPushChannel()
    {
        // snippet.channel_un_register_push
        var getChannel = await chat.GetChannel("some_channel");
        if (getChannel.Error)
        {
            Debug.LogError($"Could not get channel! Error: {getChannel.Exception.Message}");
            return;
        }
        var channel = getChannel.Result;

        var result = await channel.UnRegisterFromPush();
        if (result.Error)
        {
            Debug.LogError($"Error when trying to unregister channel from push: {result.Exception.Message}");
        }
        
        //Alternatively you can also use:
        await chat.UnRegisterPushChannels(new List<string>() { channel.Id });
        // snippet.end
    }
    
    public static async Task UnRegisterAllPushChannels()
    {
        // snippet.push_un_register_all
        var result = await chat.UnRegisterAllPushChannels();
        if (result.Error)
        {
            Debug.LogError($"Error when trying to unregister all push channels: {result.Exception.Message}");
        }
        // snippet.end
    }
    
    public static async Task GetPushChannels()
    {
        // snippet.get_all_push_channels
        var getPushChannels = await chat.GetPushChannels();
        if (getPushChannels.Error)
        {
            Debug.LogError($"Error when trying to get all push channels: {getPushChannels.Exception.Message}");
        }
        foreach (var channelId in getPushChannels.Result)
        {
            Debug.Log($"Found push channel with ID: {channelId}");
        }
        // snippet.end
    }
    
    public static async Task SendTextCustomPushData()
    {
        // snippet.send_text_push
        var getChannel = await chat.GetChannel("some_channel");
        if (getChannel.Error)
        {
            Debug.LogError($"Could not get channel! Error: {getChannel.Exception.Message}");
            return;
        }
        var channel = getChannel.Result;

        await channel.SendText("some message",
            new SendTextParams()
                { CustomPushData = new Dictionary<string, string>() { { "some_key", "some_value" } } });
        // snippet.end
    }
    
    
}