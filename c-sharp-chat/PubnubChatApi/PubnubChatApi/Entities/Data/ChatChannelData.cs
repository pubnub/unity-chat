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
        public string ChannelCustomDataJson { get; set; } = string.Empty;
        public string ChannelUpdated { get; set; } = string.Empty;
        public string ChannelStatus { get; set; } = string.Empty;
        public string ChannelType { get; set; } = string.Empty;
        
        /*public static implicit operator ChatChannelData(PNGetChannelMetadataResult metadataResult)
        {
            return new ChatChannelData()
            {
                ChannelName = metadataResult.Name,
                ChannelDescription = metadataResult.Description,
                ChannelCustomDataJson = metadataResult.Custom.TryGetValue("custom", out var custom) ? custom.ToString() : string.Empty,
                ChannelStatus = metadataResult.Custom.TryGetValue("status", out var status) ? status.ToString() : string.Empty,
                ChannelUpdated = metadataResult.Custom.TryGetValue("updated", out var updated)
                    ? updated.ToString()
                    : string.Empty,
                ChannelType = metadataResult.Custom.TryGetValue("type", out var dataType)
                    ? dataType.ToString()
                    : string.Empty,
            };
        }*/
        
        public static implicit operator ChatChannelData(PNChannelMetadataResult metadataResult)
        {
            return new ChatChannelData()
            {
                ChannelName = metadataResult.Name,
                ChannelDescription = metadataResult.Description,
                ChannelCustomDataJson = metadataResult.Custom.TryGetValue("custom", out var custom) ? custom.ToString() : string.Empty,
                ChannelStatus = metadataResult.Custom.TryGetValue("status", out var status) ? status.ToString() : string.Empty,
                ChannelUpdated = metadataResult.Updated,
                ChannelType = metadataResult.Custom.TryGetValue("type", out var dataType)
                    ? dataType.ToString()
                    : string.Empty,
            };
        }
    }
}