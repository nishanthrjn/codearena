// Placeholder for ExecutionHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace CodeArena.Api.Hubs;

[Authorize]
public class ExecutionHub(IConnectionMultiplexer redis) : Hub
{
    private const string OwnerKey = "codearena:job-owner:";

    // H-3 / L-2: Validate that the job belongs to the requesting user before subscribing.
    // This prevents any authenticated user from eavesdropping on another user's execution output.
    public async Task JoinJob(string jobId)
    {
        if (!Guid.TryParse(jobId, out var parsedJobId))
        {
            // Clients should always send a valid UUID
            throw new HubException("Invalid jobId format.");
        }

        var userId = GetUserId();
        var db = redis.GetDatabase();
        var owner = await db.StringGetAsync($"{OwnerKey}{parsedJobId}");

        if (!owner.HasValue || owner.ToString() != userId.ToString())
        {
            // Do not reveal whether the job exists — just deny
            throw new HubException("Job not found or access denied.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"job:{parsedJobId}");
    }

    public async Task LeaveJob(string jobId)
    {
        if (Guid.TryParse(jobId, out var parsed))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job:{parsed}");
    }

    // M-2: null-safe claim extraction
    private Guid GetUserId()
    {
        var val = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? Context.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(val, out var id))
            throw new HubException("Invalid user identity.");
        return id;
    }
}