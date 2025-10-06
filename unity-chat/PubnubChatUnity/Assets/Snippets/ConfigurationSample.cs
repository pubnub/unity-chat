// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class ConfigurationSample
{
    public static async Task BasicInitializationExample()
    {
        // snippet.basic_initialization_example
        var pnConfiguration = new PNConfiguration(new UserId("userId"))
        {
            PublishKey = "publishKey",
            SubscribeKey = "subscribeKey"
        };

        var chatConfig = new PubnubChatConfig(storeUserActivityTimestamp: true);

        var chatResult = await UnityChat.CreateInstance(chatConfig, pnConfiguration);

        if (!chatResult.Error)
        {
            var chatInstance = chatResult.Result;
            Debug.Log("Chat instance created successfully!");
        }
        else
        {
            Debug.LogError($"Failed to create chat instance");
        }
        // snippet.end
    }
    
    public static async Task WebGLInitializationExample()
    {
        // snippet.webgl_initialization_example
        var pnConfiguration = new PNConfiguration(new UserId("userId"))
        {
            PublishKey = "publishKey",
            SubscribeKey = "subscribeKey"
        };

        var chatConfig = new PubnubChatConfig(storeUserActivityTimestamp: true);

        var chatResult = await UnityChat.CreateInstance(chatConfig, pnConfiguration, webGLBuildMode: true);

        if (!chatResult.Error)
        {
            var chatInstance = chatResult.Result;
            Debug.Log("Chat instance created successfully for WebGL!");
        }
        else
        {
            Debug.LogError($"Failed to create chat instance");
        }
        // snippet.end
    }
}
