using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi.Entities.Data
{
    /// <summary>
    /// Data class for the chat channel.
    /// <para>
    /// Contains all the data related to the chat channel.
    /// </para>
    /// </summary>
    /// <remarks>
    /// By default, all the properties are set to empty strings.
    /// </remarks>
    public class ChatChannelData
    {
        public string ChannelName { get; set; } = string.Empty;
        public string ChannelDescription { get; set; } = string.Empty;
        public Dictionary<string, object> ChannelCustomData { get; set; } = new ();
        public string ChannelUpdated { get; set; } = string.Empty;
        public string ChannelStatus { get; set; } = string.Empty;
        public string ChannelType { get; set; } = string.Empty;
        
        public static implicit operator ChatChannelData(PNChannelMetadataResult metadataResult)
        {
            return new ChatChannelData()
            {
                ChannelName = metadataResult.Name,
                ChannelDescription = metadataResult.Description,
                ChannelCustomData = metadataResult.Custom,
                ChannelStatus = metadataResult.Status,
                ChannelUpdated = metadataResult.Updated,
                ChannelType = metadataResult.Type
            };
        }
    }
}