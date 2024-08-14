using System;
using System.Runtime.InteropServices;
using PubnubChatApi.Enums;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public class ChatAccessManager
    {
        #region DLL Imports

        [DllImport("pubnub-chat")]
        private static extern int pn_pam_can_i(IntPtr chat, byte permission, byte resource_type, string resource_name);

        #endregion

        private IntPtr chatPointer;

        internal ChatAccessManager(IntPtr chatPointer)
        {
            this.chatPointer = chatPointer;
        }

        public bool CanI(PubnubAccessPermission permission, PubnubAccessResourceType resourceType, string resourceName)
        {
            var result = pn_pam_can_i(chatPointer, (byte)permission, (byte)resourceType, resourceName);
            CUtilities.CheckCFunctionResult(result);
            return result == 1;
        }
    }
}