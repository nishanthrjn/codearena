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
    // H-4: userId required so we can enforce ownership before returning results
    Task<ExecutionResultDto?> GetResultAsync(Guid userId, Guid jobId, CancellationToken ct = default);
}

public class ExecutionService(IConnectionMultiplexer redis, ILogger<ExecutionService> log)
    : IExecutionService
{
    private const string QueueKey = "codearena:jobs";
    private const string ResultsKey = "codearena:results:";
    // H-4: track job owner in a separate key so we can enforce IDOR protection on polling
    private const string OwnerKey  = "codearena:job-owner:";

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
        return await PushJobAsync(job, userId, ct);
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
        return await PushJobAsync(job, userId, ct);
    }

    private async Task<Guid> PushJobAsync(ExecutionJob job, Guid userId, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(job);
        await db.ListRightPushAsync(QueueKey, json);
        // H-4: persist owner so results endpoint can verify the same user polls for results
        await db.StringSetAsync($"{OwnerKey}{job.Id}", userId.ToString(),
            TimeSpan.FromMinutes(15));   // TTL slightly longer than result TTL
        log.LogInformation("Enqueued job {JobId} type={Type} lang={Lang}", job.Id, job.JobType, job.Language);
        return job.Id;
    }

    public async Task<ExecutionResultDto?> GetResultAsync(Guid userId, Guid jobId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        // H-4: verify job belongs to the requesting user before returning result
        var owner = await db.StringGetAsync($"{OwnerKey}{jobId}");
        if (!owner.HasValue || owner.ToString() != userId.ToString())
            return null;   // treat as not found (caller returns 404 or pending)
        var raw = await db.StringGetAsync($"{ResultsKey}{jobId}");
        if (!raw.HasValue) return null;
        return JsonSerializer.Deserialize<ExecutionResultDto>(raw!);
    }
}