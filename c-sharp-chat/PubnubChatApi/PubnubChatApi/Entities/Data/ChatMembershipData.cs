using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi
{
    /// <summary>
    /// Data class for a chat membership.
    /// <para>
    /// Contains all the additional data related to the chat membership.
    /// </para>
    /// </summary>
    /// <remarks>
    /// By default, all the properties are set to empty strings.
    /// </remarks>
    public class ChatMembershipData
    {
        public Dictionary<string, object> CustomData { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        
        public static implicit operator ChatMembershipData(PNChannelMembersItemResult membersItem)
        {
            return new ChatMembershipData()
            {
                CustomData = membersItem.Custom,
                Status = membersItem.Status,
                Type = membersItem.Type
            };
        }
    }
}