// Placeholder for Program.cs
using CodeArena.Runner;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console())
    .ConfigureServices((ctx, services) =>
    {
        var redis = ConnectionMultiplexer.Connect(ctx.Configuration.GetConnectionString("Redis")!);
        services.AddSingleton<IConnectionMultiplexer>(redis);

        services.Configure<SandboxOptions>(ctx.Configuration.GetSection("Sandbox"));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IConfiguration>().GetSection("Sandbox").Get<SandboxOptions>()
            ?? new SandboxOptions());
        services.AddSingleton<DockerExecutor>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();