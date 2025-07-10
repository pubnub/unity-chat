using PubnubApi;

namespace PubnubChatApi.Entities.Data
{
    /// <summary>
    /// Data class for the chat user.
    /// <para>
    /// Contains all the data related to the chat user.
    /// </para>
    /// </summary>
    /// <remarks>
    /// By default, all the properties are set to empty strings.
    /// </remarks>
    public class ChatUserData
    {
        public string Username { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string ProfileUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CustomDataJson { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        public static implicit operator ChatUserData(PNUuidMetadataResult metadataResult)
        {
            return new ChatUserData()
            {
                ExternalId = metadataResult.ExternalId,
                Email = metadataResult.Email,
                ProfileUrl = metadataResult.ProfileUrl,
                Username = metadataResult.Name,
                Status = metadataResult.Custom.TryGetValue("Status", out var status) ? status.ToString() : string.Empty,
                Type = metadataResult.Custom.TryGetValue("DataType", out var dataType)
                    ? dataType.ToString()
                    : string.Empty,
                CustomDataJson = metadataResult.Custom.TryGetValue("CustomDataJson", out var custom)
                    ? custom.ToString()
                    : string.Empty,
            };
        }
        
    }
}