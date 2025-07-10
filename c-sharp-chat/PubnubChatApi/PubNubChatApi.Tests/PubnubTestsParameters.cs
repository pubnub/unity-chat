namespace PubNubChatApi.Tests;

public static class PubnubTestsParameters
{
    private static readonly string EnvPublishKey = Environment.GetEnvironmentVariable("PN_PUB_KEY");
    private static readonly string EnvSubscribeKey = Environment.GetEnvironmentVariable("PN_SUB_KEY");
    private static readonly string EnvSecretKey = Environment.GetEnvironmentVariable("PN_SEC_KEY");

    public static readonly string PublishKey = string.IsNullOrEmpty(EnvPublishKey) ? "pub-c-b23194b7-f485-43dc-9544-115997a7d9a9" : EnvPublishKey;
    public static readonly string SubscribeKey = string.IsNullOrEmpty(EnvSubscribeKey) ? "sub-c-eaa73d83-9ed7-4e49-8b5a-1b1d64160735" : EnvSubscribeKey;
    public static readonly string SecretKey = string.IsNullOrEmpty(EnvSecretKey) ? "demo-36" : EnvSecretKey;
}