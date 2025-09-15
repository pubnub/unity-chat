using PubnubApi.PNSDK;

namespace PubnubApi.Unity
{
    public class UnityChatPNSDKSource : IPNSDKSource
    {
        private const string build = "0.4.5";

        public string GetPNSDK() {
#if(UNITY_IOS)
			        return string.Format("PubNub-CSharp-Chat-UnityIOS/{0}", build);
#elif(UNITY_STANDALONE_WIN)
            return string.Format("PubNub-CSharp-Chat-UnityWin/{0}", build);
#elif(UNITY_STANDALONE_OSX)
			        return string.Format("PubNub-CSharp-Chat-UnityOSX/{0}", build);
#elif(UNITY_ANDROID)
			        return string.Format("PubNub-CSharp-Chat-UnityAndroid/{0}", build);
#elif(UNITY_STANDALONE_LINUX)
			        return string.Format("PubNub-CSharp-Chat-UnityLinux/{0}", build);
#elif(UNITY_WEBPLAYER)
			        return string.Format("PubNub-CSharp-Chat-UnityWeb/{0}", build);
#elif(UNITY_WEBGL)
					return string.Format("PubNub-CSharp-Chat-UnityWebGL/{0}", build);
#else
			        return string.Format("PubNub-CSharp-Chat-Unity/{0}", build);
#endif
        }
    }
}
