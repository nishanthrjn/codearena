// Placeholder for ExecutionServiceTests.cs
using CodeArena.Api.DTOs;
using CodeArena.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace CodeArena.Api.Tests.Services;

/// <summary>
/// Unit tests for ExecutionService — enqueue and result-retrieval paths.
/// Redis is faked with StackExchange.Redis mocks so no live server is needed.
/// </summary>
public class ExecutionServiceTests
{
    private static (ExecutionService svc, Mock<IDatabase> dbMock) Build()
    {
        var dbMock = new Mock<IDatabase>();
        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                 .Returns(dbMock.Object);

        var svc = new ExecutionService(redisMock.Object,
                                       NullLogger<ExecutionService>.Instance);
        return (svc, dbMock);
    }

    [Fact]
    public async Task EnqueueRunAsync_PushesJobToRedis()
    {
        var (svc, dbMock) = Build();

        dbMock.Setup(d => d.ListRightPushAsync(
                "codearena:jobs", It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
              .ReturnsAsync(1);

        var req = new RunRequest("python", "print('hello')", "");
        var jobId = await svc.EnqueueRunAsync(Guid.NewGuid(), req);

        Assert.NotEqual(Guid.Empty, jobId);
        dbMock.Verify(d => d.ListRightPushAsync(
            "codearena:jobs", It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task EnqueueTestAsync_PushesJobWithTestCasesJson()
    {
        var (svc, dbMock) = Build();

        RedisValue capturedValue = default;
        dbMock.Setup(d => d.ListRightPushAsync(
                "codearena:jobs", It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
              .Callback<RedisKey, RedisValue, When, CommandFlags>((_, v, _, _) => capturedValue = v)
              .ReturnsAsync(1);

        var testCases = new List<TestCaseDto>
        {
            new(null, "Case 1", "2", "4", 0),
            new(null, "Case 2", "3", "6", 1),
        };
        var req = new TestRequest("python", "n=int(input());print(n*2)", testCases);
        var jobId = await svc.EnqueueTestAsync(Guid.NewGuid(), req);

        Assert.NotEqual(Guid.Empty, jobId);
        var json = (string)capturedValue!;
        Assert.Contains("\"JobType\":\"test\"", json);
        Assert.Contains("\"TestCasesJson\"", json);
    }

    [Fact]
    public async Task GetResultAsync_ReturnsNullWhenNotFound()
    {
        var (svc, dbMock) = Build();
        var jobId = Guid.NewGuid();

        dbMock.Setup(d => d.StringGetAsync(
                $"codearena:results:{jobId}", It.IsAny<CommandFlags>()))
              .ReturnsAsync(RedisValue.Null);

        var result = await svc.GetResultAsync(jobId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetResultAsync_DeserializesResultWhenFound()
    {
        var (svc, dbMock) = Build();
        var jobId = Guid.NewGuid();

        var dto = new ExecutionResultDto(jobId, "done", "Hello\n", "", 0, 42, null);
        var json = JsonSerializer.Serialize(dto);

        dbMock.Setup(d => d.StringGetAsync(
                $"codearena:results:{jobId}", It.IsAny<CommandFlags>()))
              .ReturnsAsync(json);

        var result = await svc.GetResultAsync(jobId);

        Assert.NotNull(result);
        Assert.Equal("done", result!.Status);
        Assert.Equal("Hello\n", result.Stdout);
        Assert.Equal(42, result.DurationMs);
    }
}
