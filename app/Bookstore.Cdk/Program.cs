using Amazon.CDK;

namespace Bookstore.Cdk;

internal sealed class Program
{
    private const string AppName = "BobsUsedBooksClassic";

    public static void Main()
    {
        var app = new App();

        var env = MakeEnv();

        var coreStack = new CoreStack(app, $"{AppName}Core", new StackProps { Env = env });
        var networkStack = new NetworkStack(app, $"{AppName}Network", new StackProps { Env = env });
        var databaseStack = new DatabaseStack(app, $"{AppName}Database", new DatabaseStackProps { Env = env, Vpc = networkStack.Vpc });
        var ecsStack = new EcsStack(app, $"{AppName}ECS", new EcsStackProps { Env = env, Vpc = networkStack.Vpc, Database = databaseStack.Database, ImageBucket = coreStack.ImageBucket, WebAppUserPool = coreStack.WebAppUserPool });

        app.Synth();
    }

    private static Environment MakeEnv(string? account = null, string? region = null)
    {
        return new Environment
        {
            Account = account ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
            Region = region ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
        };
    }
}
