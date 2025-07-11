namespace PubNubChatApi.Tests;

public static class PubnubTestsParameters
{
    private static readonly string EnvPublishKey = Environment.GetEnvironmentVariable("PN_PUB_KEY");
    private static readonly string EnvSubscribeKey = Environment.GetEnvironmentVariable("PN_SUB_KEY");
    private static readonly string EnvSecretKey = Environment.GetEnvironmentVariable("PN_SEC_KEY");

    public static readonly string PublishKey = string.IsNullOrEmpty(EnvPublishKey) ? "demo-36" : EnvPublishKey;
    public static readonly string SubscribeKey = string.IsNullOrEmpty(EnvSubscribeKey) ? "demo-36" : EnvSubscribeKey;
    public static readonly string SecretKey = string.IsNullOrEmpty(EnvSecretKey) ? "demo-36" : EnvSecretKey;
}