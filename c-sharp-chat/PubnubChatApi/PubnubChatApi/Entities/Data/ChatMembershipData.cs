namespace PubnubChatApi.Entities.Data
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
        public string CustomDataJson { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}