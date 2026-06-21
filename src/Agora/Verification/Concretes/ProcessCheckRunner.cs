using Agora.Verification.Contracts;
using Agora.Verification.Models;
using Agora.Verification.Concretes;
using System.Diagnostics;
using System.Text;
using Agora.Configuration;

namespace Agora.Verification.Concretes;

/// <summary>
/// Runs configured checks as real OS processes — no shell, so the executable and its arguments are
/// never re-parsed and supplied values can't inject commands. Only checks present in
/// <see cref="ChecksConfig.Commands"/> can run; <c>{key}</c> placeholders in a check's argument tokens
/// are replaced token-by-token with the caller's values. Captures stdout+stderr (excerpted), enforces
/// a per-check timeout (killing the process tree on expiry), and reports the exit code.
/// </summary>
public sealed class ProcessCheckRunner : ICheckRunner
{
    private const int MaxOutputChars = 4000;

    private readonly IReadOnlyDictionary<string, CheckCommandConfig> _commands;
    private readonly TimeSpan _timeout;

    /// <inheritdoc />
    public string Workdir { get; }

    /// <summary>Builds the runner from the checks config, resolving the working directory relative to
    /// <paramref name="baseDir"/> (the config directory).</summary>
    public ProcessCheckRunner(ChecksConfig config, string baseDir = "")
    {
        _commands = config.Commands;
        _timeout = TimeSpan.FromSeconds(config.Timeout <= 0 ? 120 : config.Timeout);
        var root = string.IsNullOrEmpty(baseDir) ? Directory.GetCurrentDirectory() : baseDir;
        Workdir = string.IsNullOrEmpty(config.Workdir)
            ? root
            : Path.GetFullPath(Path.IsPathRooted(config.Workdir) ? config.Workdir : Path.Combine(root, config.Workdir));
    }

    /// <inheritdoc />
    public bool HasCheck(string name) => _commands.ContainsKey(name);

    /// <inheritdoc />
    public async Task<CheckResult> RunAsync(
        string checkName, IReadOnlyDictionary<string, string> arguments, CancellationToken cancellationToken = default)
    {
        if (!_commands.TryGetValue(checkName, out var command))
            return new CheckResult(checkName, false, -1, $"unknown check '{checkName}'", 0);

        var psi = new ProcessStartInfo
        {
            FileName = command.Command,
            WorkingDirectory = Workdir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in command.Args)
            psi.ArgumentList.Add(Substitute(arg, arguments));

        var stopwatch = Stopwatch.StartNew();
        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
        }
        catch (Exception e)
        {
            return new CheckResult(checkName, false, -1, $"failed to start '{command.Command}': {e.Message}", stopwatch.ElapsedMilliseconds);
        }

        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new CheckResult(checkName, false, -1, $"timed out after {_timeout.TotalSeconds:0}s", stopwatch.ElapsedMilliseconds);
        }

        var output = Excerpt(await stdout, await stderr);
        return new CheckResult(checkName, process.ExitCode == 0, process.ExitCode, output, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>Replaces <c>{key}</c> occurrences in a single argument token with supplied values.</summary>
    private static string Substitute(string arg, IReadOnlyDictionary<string, string> arguments)
    {
        if (arguments.Count == 0 || !arg.Contains('{'))
            return arg;
        foreach (var (key, value) in arguments)
            arg = arg.Replace("{" + key + "}", value);
        return arg;
    }

    private static string Excerpt(string stdout, string stderr)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(stdout)) sb.Append(stdout);
        if (!string.IsNullOrWhiteSpace(stderr)) sb.Append(sb.Length > 0 ? "\n" : "").Append(stderr);
        var text = sb.ToString().Trim();
        return text.Length > MaxOutputChars ? text[^MaxOutputChars..] : text;
    }

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch { /* best-effort */ }
    }
}
