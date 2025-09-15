namespace PubnubChatApi.Entities.Data
{
    /// <summary>
    /// Data struct for restriction.
    /// </summary>
    public class Restriction
    {
        public bool Ban;
        public bool Mute;
        public string Reason = string.Empty;
    }
    
    public class UserRestriction : Restriction
    {
        public string UserId = string.Empty;
    }
    
    public class ChannelRestriction : Restriction
    {
        public string ChannelId = string.Empty;
    }
}