using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public class ChatAccessManager
    {
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern int pn_pam_can_i(IntPtr chat, byte permission, byte resource_type, string resource_name);
        
        [DllImport("pubnub-chat")]
        private static extern int pn_pam_parse_token(IntPtr chat, string token, StringBuilder result);

        [DllImport("pubnub-chat")]
        private static extern int pn_pam_set_auth_token(IntPtr chat, string token);

        [DllImport("pubnub-chat")]
        private static extern int pn_pam_set_pubnub_origin(IntPtr chat, string origin);

        #endregion

        private IntPtr chatPointer;

        internal ChatAccessManager(IntPtr chatPointer)
        {
            this.chatPointer = chatPointer;
        }

        public async Task<bool> CanI(PubnubAccessPermission permission, PubnubAccessResourceType resourceType, string resourceName)
        {
            var result = await Task.Run(() => pn_pam_can_i(chatPointer, (byte)permission, (byte)resourceType, resourceName));
            CUtilities.CheckCFunctionResult(result);
            return result == 1;
        }
        
        /// <summary>
        /// Sets a new token for this Chat instance.
        /// </summary>
        public void SetAuthToken(string token)
        {
            CUtilities.CheckCFunctionResult(pn_pam_set_auth_token(chatPointer, token));
        }

        /// <summary>
        ///  Decodes an existing token.
        /// </summary>
        /// <returns>A JSON string object containing permissions embedded in that token.</returns>
        public string ParseToken(string token)
        {
            var buffer = new StringBuilder(512);
            CUtilities.CheckCFunctionResult(pn_pam_parse_token(chatPointer, token, buffer));
            return buffer.ToString();
        }

        /// <summary>
        /// Sets a new custom origin value.            
        /// </summary>
        /// <param name="origin"></param>
        public void SetPubnubOrigin(string origin)
        {
            CUtilities.CheckCFunctionResult(pn_pam_set_pubnub_origin(chatPointer, origin));
        }
    }
}