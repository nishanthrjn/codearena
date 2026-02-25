// Worker.cs
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using StackExchange.Redis;

namespace CodeArena.Runner;

public class Worker(
    IConnectionMultiplexer redis,
    DockerExecutor executor,
    SandboxOptions opts,
    IConfiguration cfg,
    ILogger<Worker> log) : BackgroundService
{
    private const string QueueKey = "codearena:jobs";
    private const string ResultsKey = "codearena:results:";
    private const int ResultTTLSeconds = 300;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation("Runner Worker started");
        var db = redis.GetDatabase();
        var hub = await BuildHubConnectionAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var raw = await db.ListLeftPopAsync(QueueKey);
                if (!raw.HasValue)
                {
                    await Task.Delay(200, stoppingToken);
                    continue;
                }

                var job = JsonSerializer.Deserialize<ExecutionJob>(raw!)!;
                log.LogInformation("Processing job {Id} type={Type} lang={Lang}", job.Id, job.JobType, job.Language);

                ExecutionResultDto result;
                if (job.JobType == "test")
                {
                    result = await RunTestJobAsync(job, hub, stoppingToken);
                }
                else
                {
                    result = await RunSingleJobAsync(job, hub, stoppingToken);
                }

                // Store result in Redis with TTL
                await db.StringSetAsync(
                    $"{ResultsKey}{job.Id}",
                    JsonSerializer.Serialize(result),
                    TimeSpan.FromSeconds(ResultTTLSeconds));

                // Notify via SignalR that job is done
                if (hub.State == HubConnectionState.Connected)
                {
                    await hub.SendAsync("JobComplete", job.Id.ToString(), result, stoppingToken);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                log.LogError(ex, "Worker error processing job");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task<ExecutionResultDto> RunSingleJobAsync(
        ExecutionJob job, HubConnection hub, CancellationToken ct)
    {
        var (stdout, stderr, exitCode, ms) = await executor.ExecuteAsync(
            job.Language, job.Code, job.StdIn, job.Id.ToString(), hub, ct);

        return new ExecutionResultDto(
            job.Id, exitCode == 0 ? "done" : "error",
            stdout, stderr, exitCode, ms, null);
    }

    private async Task<ExecutionResultDto> RunTestJobAsync(
        ExecutionJob job, HubConnection hub, CancellationToken ct)
    {
        var testCases = JsonSerializer.Deserialize<List<TestCaseDto>>(job.TestCasesJson) ?? [];
        var results = new List<TestCaseResultDto>();
        long totalMs = 0;

        foreach (var tc in testCases.Take(opts.MaxTestCases))
        {
            var (stdout, stderr, exitCode, ms) = await executor.ExecuteAsync(
                job.Language, job.Code, tc.StdIn, $"{job.Id}-{tc.OrderIndex}", hub, ct);
            totalMs += ms;

            var passed = exitCode == 0 &&
                         stdout.TrimEnd().Equals(tc.Expected.TrimEnd(), StringComparison.Ordinal);
            results.Add(new TestCaseResultDto(tc.Name, passed, stdout, stderr, tc.Expected, ms));
        }

        var allPassed = results.All(r => r.Passed);
        return new ExecutionResultDto(
            job.Id, allPassed ? "done" : "error",
            null, null, allPassed ? 0 : 1, totalMs, results);
    }

    private async Task<HubConnection> BuildHubConnectionAsync(CancellationToken ct)
    {
        var hubUrl = cfg["Api:HubUrl"] ?? "http://api:5000/hubs/execution";
        var conn = new HubConnectionBuilder()
            .WithUrl(hubUrl, opts => opts.AccessTokenProvider = () =>
                Task.FromResult<string?>(cfg["Api:RunnerToken"]))
            .WithAutomaticReconnect()
            .Build();
        try
        {
            await conn.StartAsync(ct);
            log.LogInformation("SignalR hub connected to {Url}", hubUrl);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not connect to SignalR hub — streaming disabled");
        }
        return conn;
    }
}