using System.Collections.Generic;
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
        public string OLD_CustomDataJson { get; set; } = string.Empty;
        public Dictionary<string, object> CustomData { get; set; } = new ();
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
                Status = metadataResult.Custom.TryGetValue("status", out var status) ? status.ToString() : string.Empty,
                Type = metadataResult.Custom.TryGetValue("type", out var dataType)
                    ? dataType.ToString()
                    : string.Empty,
                //TODO: won't work? cause it's not a json string i think. to be removed anyway
                OLD_CustomDataJson = metadataResult.Custom.TryGetValue("custom", out var customJson)
                    ? customJson.ToString()
                    : string.Empty,
                CustomData = metadataResult.Custom.TryGetValue("custom", out var custom) ? (Dictionary<string, object>)custom : new ()
            };
        }
        
    }
}