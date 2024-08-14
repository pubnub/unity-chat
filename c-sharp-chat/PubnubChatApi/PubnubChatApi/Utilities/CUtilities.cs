using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PubnubChatApi.Utilities
{
    public class PubNubCCoreException : Exception
    {
        public PubNubCCoreException(string message) : base(message)
        {
        }
    }
    
    internal static class CUtilities
    {
        [DllImport("pubnub-chat")]
        private static extern void pn_c_get_error_message(StringBuilder buffer);

        private static void ThrowCError()
        {
            var errorMessage = GetErrorMessage();
            Debug.WriteLine($"Throwing C-side Error: {errorMessage}");
            throw new PubNubCCoreException(errorMessage);
        }

        internal static string GetErrorMessage()
        {
            var buffer = new StringBuilder(4096);
            pn_c_get_error_message(buffer);
            return buffer.ToString();
        }

        internal static void CheckCFunctionResult(int result)
        {
            if (result == -1)
            {
                ThrowCError();
            }
        }

        internal static void CheckCFunctionResult(IntPtr result)
        {
            if (result == IntPtr.Zero)
            {
                ThrowCError();
            }
        }

        internal static bool IsValidJson(string json)
        {
            return !string.IsNullOrEmpty(json) && json != "{}" && json != "[]";
        }
    }
}
