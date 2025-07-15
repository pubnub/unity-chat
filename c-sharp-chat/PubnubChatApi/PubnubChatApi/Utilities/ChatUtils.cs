using System;
using System.Globalization;

namespace PubnubChatApi.Utilities
{
    internal static class ChatUtils
    {
        internal static string TimeTokenNow()
        {
            var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timeStamp = Convert.ToInt64(timeSpan.TotalSeconds);
            return timeStamp.ToString(CultureInfo.InvariantCulture);
        }
    }
}