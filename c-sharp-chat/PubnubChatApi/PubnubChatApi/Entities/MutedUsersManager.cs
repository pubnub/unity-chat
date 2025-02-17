using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PubNubChatAPI.Entities
{
    /// <summary>
    /// <para>An object for manipulating the list of muted users.</para>
    /// <para>The list is local to this instance of Chat (it is not persisted anywhere) unless
    /// PubnubChatConfig.SyncMutedUsers is enabled, in which case it will be synced using App Context for the current
    /// user.</para>
    /// <para>Please note that this is not a server-side moderation mechanism (use Chat.SetRestrictions or Channel.SetRestrictions for that), but rather
    /// a way to ignore messages from certain users on the client.</para>
    /// </summary>
    public class MutedUsersManager
    {
        #region DLL Imports

        /*[DllImport("pubnub-chat")]
        private static extern int pn_pam_can_i(IntPtr chat, byte permission, byte resource_type, string resource_name);*/

        #endregion

        private IntPtr chatPointer;

        internal MutedUsersManager(IntPtr chatPointer)
        {
            this.chatPointer = chatPointer;
        }

        /// <summary>
        /// The current set of muted users.
        /// </summary>
        /// <returns></returns>
        public List<string> MutedUsers { get; }
        
        /// <summary>
        /// Add a user to the list of muted users.
        /// <para>When PubnubChatConfig.SyncMutedUsers is enabled, it can fail e.g. because of network 
        /// conditions or when number of muted users exceeds the limit.</para>
        /// <para>When SyncMutedUsers is false, it always succeeds (data is not synced in that case).</para>
        /// </summary>
        /// <param name="userId">The ID of the user to mute</param>
        public async Task MuteUser(string userId)
        { 
            
        }

        /// <summary>
        /// Remove a user from the list of muted users.
        /// <para>When PubnubChatConfig.SyncMutedUsers is enabled, it can fail e.g. because of network 
        /// conditions or when number of muted users exceeds the limit.</para>
        /// <para>When SyncMutedUsers is false, it always succeeds (data is not synced in that case).</para>
        /// </summary>
        /// <param name="userId">The ID of the user to unmute</param>
        public async Task UnmuteUser(string userId)
        {
            
        }
    }
}