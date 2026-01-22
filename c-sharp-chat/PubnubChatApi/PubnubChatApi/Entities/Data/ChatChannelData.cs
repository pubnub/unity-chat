using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi
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
        internal static string RECEIPTS_FLAG => $"{Chat.INTERNAL_DATA_PREFIX}{"EmitReadReceipts"}";
        
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> CustomData { get; set; } = new ();
        public string Updated { get; internal set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        
        public bool? EmitReadReceiptEvents
        {
            get
            {
                if (!CustomData.TryGetValue(RECEIPTS_FLAG, out var value))
                {
                    return null;
                }
                return (bool)value;
            }
            set => CustomData[RECEIPTS_FLAG] = true;
        }

        public static implicit operator ChatChannelData(PNChannelMetadataResult metadataResult)
        {
            return new ChatChannelData()
            {
                Name = metadataResult.Name,
                Description = metadataResult.Description,
                CustomData = metadataResult.Custom,
                Status = metadataResult.Status,
                Updated = metadataResult.Updated,
                Type = metadataResult.Type
            };
        }
    }
}