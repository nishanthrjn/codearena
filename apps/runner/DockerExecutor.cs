// DockerExecutor.cs
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;

namespace CodeArena.Runner;

public class DockerExecutor(SandboxOptions opts, ILogger<DockerExecutor> log)
{
    public async Task<(string Stdout, string Stderr, int ExitCode, long DurationMs)> ExecuteAsync(
        string language,
        string code,
        string stdin,
        string jobId,
        HubConnection? hub = null,
        CancellationToken ct = default)
    {
        var langCfg = LanguageRegistry.Get(language);
        var workspace = Path.Combine(opts.WorkspaceBasePath, jobId);
        Directory.CreateDirectory(workspace);

        try
        {
            // Write source code to workspace (host path, will be bind-mounted read-only)
            var srcPath = Path.Combine(workspace, langCfg.FileName);
            await File.WriteAllTextAsync(srcPath, code, ct);

            // Compile step (if needed)
            if (langCfg.CompileCommand is not null)
            {
                var compileResult = await RunContainerAsync(
                    langCfg.Image, workspace, langCfg.CompileCommand, "", jobId, hub, isCompile: true, ct);
                if (compileResult.ExitCode != 0)
                    return (compileResult.Stdout, $"Compile error:\n{compileResult.Stderr}",
                            compileResult.ExitCode, compileResult.DurationMs);
            }

            // Run step
            return await RunContainerAsync(
                langCfg.Image, workspace, langCfg.RunCommand, stdin, jobId, hub, isCompile: false, ct);
        }
        finally
        {
            // Always clean up workspace
            try { Directory.Delete(workspace, recursive: true); } catch { /* best effort */ }
        }
    }

    private async Task<(string Stdout, string Stderr, int ExitCode, long DurationMs)> RunContainerAsync(
        string image, string workspace, string command,
        string stdin, string jobId, HubConnection? hub, bool isCompile, CancellationToken ct)
    {
        // Build docker run command with strict sandbox flags
        var dockerArgs = new StringBuilder();
        dockerArgs.Append("run --rm");
        dockerArgs.Append(" --network=none");                                           // no outbound network
        dockerArgs.Append($" --memory={opts.MemoryLimitBytes}");
        dockerArgs.Append($" --memory-swap={opts.MemoryLimitBytes}");                  // no swap
        dockerArgs.Append($" --cpu-period={opts.CpuPeriodMicroseconds}");
        dockerArgs.Append($" --cpu-quota={opts.CpuQuotaMicroseconds}");
        dockerArgs.Append(" --read-only");                                              // read-only root FS
        dockerArgs.Append($" --tmpfs /workspace:rw,size=64m,exec");                    // writable /workspace tmpfs (exec needed for compiled binaries)
        dockerArgs.Append(" --tmpfs /tmp:rw,size=32m");
        dockerArgs.Append(" --cap-drop=ALL");                                           // drop all Linux caps
        dockerArgs.Append(" --security-opt no-new-privileges");
        dockerArgs.Append(" --pids-limit=100");                                         // prevent fork bombs
        dockerArgs.Append($" -v {workspace}:/workspace-src:ro");                        // source read-only
        dockerArgs.Append(" -i");                                                        // stdin support
        dockerArgs.Append($" --name=ca-job-{jobId[..8]}-{(isCompile ? "compile" : "run")}");

        // Copy source into /workspace tmpfs using entrypoint wrapper
        // We mount source as read-only and copy into the writable tmpfs at /workspace
        dockerArgs.Append($" {image}");
        dockerArgs.Append($" sh -c \"cp -r /workspace-src/. /workspace/ && {command}\"");

        var psi = new ProcessStartInfo("docker", dockerArgs.ToString())
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var sw = Stopwatch.StartNew();
        using var proc = Process.Start(psi)!;

        // Write stdin (fire and forget, close stdin after)
        var stdinTask = Task.Run(async () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(stdin))
                    await proc.StandardInput.WriteAsync(stdin);
                proc.StandardInput.Close();
            }
            catch { /* process may exit early */ }
        }, ct);

        // Stream output with size limits
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();
        var outBytes = new long[] { 0 };  // long[] so async tasks can share state without ref

        var stdoutTask = ReadStreamAsync(proc.StandardOutput, stdoutBuilder, outBytes, jobId, hub, ct);
        var stderrTask = ReadStreamAsync(proc.StandardError, stderrBuilder, outBytes, jobId, hub, ct);

        // Enforce timeout
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(opts.TimeoutSeconds));

        try
        {
            await proc.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            log.LogWarning("Job {JobId} timed out — killing container", jobId);
            try { proc.Kill(true); } catch { /* already dead */ }
            // Force-remove container
            await RunDockerKillAsync($"ca-job-{jobId[..8]}-{(isCompile ? "compile" : "run")}");
            return ("", $"Execution timed out after {opts.TimeoutSeconds}s", 124, sw.ElapsedMilliseconds);
        }

        await Task.WhenAll(stdoutTask, stderrTask, stdinTask);
        sw.Stop();

        return (stdoutBuilder.ToString(), stderrBuilder.ToString(), proc.ExitCode, sw.ElapsedMilliseconds);
    }

    private async Task ReadStreamAsync(
        StreamReader reader, StringBuilder builder,
        long[] byteCount, string jobId, HubConnection? hub, CancellationToken ct)
    {
        var buf = new char[4096];
        int read;
        while ((read = await reader.ReadAsync(buf, 0, buf.Length)) > 0)
        {
            var chunk = new string(buf, 0, read);
            byteCount[0] += read;
            if (byteCount[0] > opts.MaxOutputBytes)
            {
                builder.Append("\n[Output truncated — limit exceeded]");
                break;
            }
            builder.Append(chunk);
            if (hub?.State == HubConnectionState.Connected)
            {
                try { await hub.SendAsync("ExecutionOutput", jobId, chunk, ct); }
                catch { /* non-fatal */ }
            }
        }
    }

    private static Task RunDockerKillAsync(string containerName)
        => Task.Run(() =>
        {
            try
            {
                using var p = Process.Start("docker", $"rm -f {containerName}")!;
                p.WaitForExit(3000);
            }
            catch { /* best effort */ }
        });
}