using System;
using System.Globalization;
using PubnubApi;

namespace PubnubChatApi
{
    public static class ChatUtils
    {
        public static string TimeTokenNow()
        {
            return TimeToken(DateTime.UtcNow);
        }
        
        public static string TimeToken(DateTime date)
        {
            var timeSpan = date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timeStamp = Convert.ToInt64(timeSpan.TotalSeconds  * 10000000);
            return timeStamp.ToString(CultureInfo.InvariantCulture);
        }
        
        internal static long TimeTokenNowLong()
        {
            var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timeStamp = Convert.ToInt64(timeSpan.TotalSeconds  * 10000000);
            return timeStamp;
        }

        internal static ChatOperationResult ToChatOperationResult<T>(this PNResult<T> result, string operationName, Chat chat)
        {
            var operationResult = new ChatOperationResult(operationName, chat);
            operationResult.RegisterOperation(result);
            return operationResult;
        }
    }
}