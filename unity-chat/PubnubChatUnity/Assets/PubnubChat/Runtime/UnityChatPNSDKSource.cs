using PubnubApi.PNSDK;
using PubnubApi.Unity;

namespace PubnubChatApi
{
    public class UnityChatPNSDKSource : IPNSDKSource
    {
        private const string build = "1.1.0";

        private string GetPlatformString()
        {
#if(UNITY_IOS)
			return "IOS";
#elif(UNITY_STANDALONE_WIN)
	        return "Win";
#elif(UNITY_STANDALONE_OSX)
			return "OSX";
#elif(UNITY_ANDROID)
			return "Android";
#elif(UNITY_STANDALONE_LINUX)
			return "Linux";
#elif(UNITY_WEBPLAYER)
			return "Web";
#elif(UNITY_WEBGL)
			return "WebGL";
#else
			return "";
#endif
        }
        
        public string GetPNSDK()
        {
	        var unitySdkVersion = new UnityPNSDKSource().Build;
	        return $"PubNub-CSharp-Unity{GetPlatformString()}/{unitySdkVersion}/CA-Unity/{build}";
        }
    }
}
