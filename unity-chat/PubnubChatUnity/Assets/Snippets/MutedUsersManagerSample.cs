// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

namespace Snippets
{
    public class MutedUsersManagerSample
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
        
        public static async Task MuteUserExample()
        {
            // snippet.mute_user
            var mutedUsersManager = chat.MutedUsersManager;
            var muteResult = await mutedUsersManager.MuteUser("some_user");
            if (muteResult.Error)
            {
                Debug.LogError($"Error when trying to mute user: {muteResult.Exception.Message}");
            }
            // snippet.end
        }
    
        public static async Task UnMuteUserExample()
        {
            // snippet.unmute_user
            var mutedUsersManager = chat.MutedUsersManager;
            var muteResult = await mutedUsersManager.UnMuteUser("some_user");
            if (muteResult.Error)
            {
                Debug.LogError($"Error when trying to unmute user: {muteResult.Exception.Message}");
            }
            // snippet.end
        }
    
        public static async Task CheckMutedExample()
        {
            // snippet.check_muted
            var mutedUsersManager = chat.MutedUsersManager;
            var mutedUsers = mutedUsersManager.MutedUsers;
            foreach (var mutedUserId in mutedUsers)
            {
                Debug.Log($"Muted user: {mutedUserId}");
            }
            // snippet.end
        }
    }
}