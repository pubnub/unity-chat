namespace PubNubChatApi.Tests;

public static class PubnubTestsParameters
{
    private static readonly string EnvPublishKey = Environment.GetEnvironmentVariable("PN_PUB_KEY");
    private static readonly string EnvSubscribeKey = Environment.GetEnvironmentVariable("PN_SUB_KEY");
    private static readonly string EnvSecretKey = Environment.GetEnvironmentVariable("PN_SEC_KEY");

    public static readonly string PublishKey = "pub-c-79c582a2-d7a4-4ee7-9f28-7a6f1b7fa11c";//string.IsNullOrEmpty(EnvPublishKey) ? "demo-36" : EnvPublishKey;
    public static readonly string SubscribeKey = "sub-c-ca0af928-f4f9-474c-b56e-d6be81bf8ed0";//string.IsNullOrEmpty(EnvSubscribeKey) ? "demo-36" : EnvSubscribeKey;
    public static readonly string SecretKey = string.IsNullOrEmpty(EnvSecretKey) ? "demo-36" : EnvSecretKey;
}