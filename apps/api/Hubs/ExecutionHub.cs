// Placeholder for ExecutionHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CodeArena.Api.Hubs;

[Authorize]
public class ExecutionHub : Hub
{
    // Clients join a group named after their jobId to receive streaming output
    public async Task JoinJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job:{jobId}");
    }

    public async Task LeaveJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job:{jobId}");
    }
}