namespace PubNubChatApi.Tests;

public static class PubnubTestsParameters
{
    private static readonly string EnvPublishKey = Environment.GetEnvironmentVariable("PN_PUB_KEY");
    private static readonly string EnvSubscribeKey = Environment.GetEnvironmentVariable("PN_SUB_KEY");
    private static readonly string EnvSecretKey = Environment.GetEnvironmentVariable("PN_SEC_KEY");
    
    public static readonly string PublishKey = string.IsNullOrEmpty(EnvPublishKey) ? "pub-c-79961364-c3e6-4e48-8d8d-fe4f34e228bf" : EnvPublishKey;
    public static readonly string SubscribeKey = string.IsNullOrEmpty(EnvSubscribeKey) ? "sub-c-2b4db8f2-c025-4a76-9e23-326123298667" : EnvSubscribeKey;
    public static readonly string SecretKey = string.IsNullOrEmpty(EnvSecretKey) ? "demo-36" : EnvSecretKey;
}