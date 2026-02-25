// Placeholder for ExecutionService.cs
using System.Text.Json;
using CodeArena.Api.DTOs;
using CodeArena.Api.Models;
using StackExchange.Redis;

namespace CodeArena.Api.Services;

public interface IExecutionService
{
    Task<Guid> EnqueueRunAsync(Guid userId, RunRequest req, CancellationToken ct = default);
    Task<Guid> EnqueueTestAsync(Guid userId, TestRequest req, CancellationToken ct = default);
    Task<ExecutionResultDto?> GetResultAsync(Guid jobId, CancellationToken ct = default);
}

public class ExecutionService(IConnectionMultiplexer redis, ILogger<ExecutionService> log)
    : IExecutionService
{
    private const string QueueKey = "codearena:jobs";
    private const string ResultsKey = "codearena:results:";

    public async Task<Guid> EnqueueRunAsync(Guid userId, RunRequest req, CancellationToken ct = default)
    {
        var job = new ExecutionJob
        {
            UserId = userId,
            Language = req.Language,
            Code = req.Code,
            StdIn = req.StdIn,
            JobType = "run"
        };
        return await PushJobAsync(job, ct);
    }

    public async Task<Guid> EnqueueTestAsync(Guid userId, TestRequest req, CancellationToken ct = default)
    {
        var job = new ExecutionJob
        {
            UserId = userId,
            Language = req.Language,
            Code = req.Code,
            JobType = "test",
            TestCasesJson = JsonSerializer.Serialize(req.TestCases)
        };
        return await PushJobAsync(job, ct);
    }

    private async Task<Guid> PushJobAsync(ExecutionJob job, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(job);
        await db.ListRightPushAsync(QueueKey, json);
        log.LogInformation("Enqueued job {JobId} type={Type} lang={Lang}", job.Id, job.JobType, job.Language);
        return job.Id;
    }

    public async Task<ExecutionResultDto?> GetResultAsync(Guid jobId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var raw = await db.StringGetAsync($"{ResultsKey}{jobId}");
        if (!raw.HasValue) return null;
        return JsonSerializer.Deserialize<ExecutionResultDto>(raw!);
    }
}