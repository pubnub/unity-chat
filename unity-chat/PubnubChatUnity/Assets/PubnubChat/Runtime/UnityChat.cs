using System.Threading.Tasks;
using PubnubApi;
using PubnubApi.Unity;
using PubNubChatAPI.Entities;
using PubnubChatApi.Entities.Data;

namespace PubnubChat.Runtime
{
    public static class UnityChat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Chat"/> class.
        /// <para>
        /// Creates a new chat instance setup for Unity environment.
        /// </para>
        /// </summary>
        /// <param name="chatConfig">Config with Chat specific parameters</param>
        /// <param name="pubnubConfig">Config with PubNub keys and values</param>
        /// <param name="webGLBuildMode">Flag for enabling WebGL mode - sets httpTransportService to UnityWebGLHttpClientService</param>
        /// <param name="unityLogging">Flag to set Unity specific logger (UnityPubNubLogger)</param>
        /// <returns>A ChatOperationResult containing the created Chat instance.</returns>
        /// <remarks>
        /// The constructor initializes the Chat object with a new Pubnub instance.
        /// </remarks>
        public static async Task<ChatOperationResult<Chat>> CreateInstance(PubnubChatConfig chatConfig, PNConfiguration pubnubConfig, bool webGLBuildMode = false, bool unityLogging = false)
        {
            var pubnub = PubnubUnityUtils.NewUnityPubnub(pubnubConfig, webGLBuildMode, unityLogging, new UnityChatPNSDKSource());
            return await Chat.CreateInstance(chatConfig, pubnub, new UnityListenerFactory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chat"/> class.
        /// <para>
        /// Creates a new chat instance setup for Unity environment.
        /// </para>
        /// </summary>
        /// <param name="chatConfig">Config with Chat specific parameters</param>
        /// <param name="configurationAsset">Pubnub configuration Scriptable Object asset</param>
        /// <param name="userId">Client user ID for this instance</param>
        /// <returns>A ChatOperationResult containing the created Chat instance.</returns>
        /// <remarks>
        /// The constructor initializes the Chat object with a new Pubnub instance.
        /// </remarks>
        public static async Task<ChatOperationResult<Chat>> CreateInstance(PubnubChatConfig chatConfig, PNConfigAsset configurationAsset, string userId)
        {
            var pubnub = PubnubUnityUtils.NewUnityPubnub(configurationAsset, userId, new UnityChatPNSDKSource());
            return await Chat.CreateInstance(chatConfig, pubnub, new UnityListenerFactory());
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Chat"/> class.
        /// <para>
        /// Creates a new chat instance setup for Unity environment.
        /// </para>
        /// </summary>
        /// <param name="chatConfig">Config with Chat specific parameters</param>
        /// <param name="pubnub">An existing Pubnub object instance</param>
        /// <returns>A ChatOperationResult containing the created Chat instance.</returns>
        /// <remarks>
        /// The constructor initializes the Chat object with an existing Pubnub instance.
        /// </remarks>
        public static async Task<ChatOperationResult<Chat>> CreateInstance(PubnubChatConfig chatConfig, Pubnub pubnub)
        {
            return await Chat.CreateInstance(chatConfig, pubnub);
        }
    }
}