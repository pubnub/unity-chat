using System;
using System.Globalization;
using PubnubApi;
using PubnubChatApi.Entities.Data;

namespace PubnubChatApi.Utilities
{
    internal static class ChatUtils
    {
        internal static string TimeTokenNow()
        {
            var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timeStamp = Convert.ToInt64(timeSpan.TotalSeconds  * 10000000);
            return timeStamp.ToString(CultureInfo.InvariantCulture);
        }
        
        internal static long TimeTokenNowLong()
        {
            var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timeStamp = Convert.ToInt64(timeSpan.TotalSeconds  * 10000000);
            return timeStamp;
        }

        internal static ChatOperationResult ToChatOperationResult<T>(this PNResult<T> result)
        {
            var operationResult = new ChatOperationResult();
            operationResult.RegisterOperation(result);
            return operationResult;
        }
    }
}