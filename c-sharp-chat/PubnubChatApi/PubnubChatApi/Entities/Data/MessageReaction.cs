using System.Collections.Generic;

namespace PubnubChatApi
{
    /// <summary>
    /// Contains the data for a single type of reaction to a specific message.
    /// </summary>
    public class MessageReaction
    {
        /// <summary>
        /// Type of reaction, e.g. an emoji
        /// </summary>
        public string Value { get; set; } = string.Empty;
        /// <summary>
        /// Whether the reaction was also made by the current user
        /// </summary>
        public bool IsMine {get; set;}
        /// <summary>
        /// All the users who gave this reaction
        /// </summary>
        public List<string> UserIds {get; set;} = new();
        /// <summary>
        /// Amount of reactions - equal to the count of UserIDs.
        /// </summary>
        public int Count => UserIds.Count;
    }
}