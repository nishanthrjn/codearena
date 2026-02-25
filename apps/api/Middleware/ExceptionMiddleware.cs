// Placeholder for ExceptionMiddleware.cs
using System.Net;
using System.Text.Json;

namespace CodeArena.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (UnauthorizedAccessException ex)
        {
            log.LogWarning(ex, "Unauthorized access on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await WriteError(ctx, HttpStatusCode.Unauthorized, "Unauthorized");
        }
        catch (KeyNotFoundException ex)
        {
            log.LogWarning(ex, "Resource not found on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await WriteError(ctx, HttpStatusCode.NotFound, "Resource not found");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await WriteError(ctx, HttpStatusCode.InternalServerError, "Internal server error");
        }
    }

    private static Task WriteError(HttpContext ctx, HttpStatusCode code, string message)
    {
        ctx.Response.StatusCode  = (int)code;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}