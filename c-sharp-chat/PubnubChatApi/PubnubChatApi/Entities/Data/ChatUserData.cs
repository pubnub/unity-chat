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
    }
}