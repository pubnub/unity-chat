using System.Globalization;
using System.Reflection;
using PubnubApi;
using PubnubApi.PNSDK;

namespace PubnubChatApi.Utilities
{
    public class PubnubChatDotNetPNSDKSource : IPNSDKSource
    {
        public string GetPNSDK()
        {
            var assembly = typeof(Pubnub).GetTypeInfo().Assembly;
            var assemblyName = new AssemblyName(assembly.FullName);
            string assemblyVersion = assemblyName.Version.ToString();
            var targetFramework = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkDisplayName?.Replace(".",string.Empty).Replace(" ", string.Empty);
            
            return string.Format(CultureInfo.InvariantCulture, "{0}/CSharpChat/{1}", targetFramework??"UNKNOWN", assemblyVersion);
        }
    }
}