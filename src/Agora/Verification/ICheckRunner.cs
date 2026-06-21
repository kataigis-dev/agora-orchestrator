namespace Agora.Verification;

/// <summary>
/// Runs a named, pre-configured check (a build/test command) and returns its real
/// <see cref="CheckResult"/>. Only checks declared in config are runnable — an agent never supplies a
/// raw command line — so the model cannot execute arbitrary shell. Implemented by
/// <see cref="ProcessCheckRunner"/>.
/// </summary>
public interface ICheckRunner
{
    /// <summary>The working directory checks run in (used to resolve FileExists criteria too).</summary>
    string Workdir { get; }

    /// <summary>True when a check with this name is configured.</summary>
    bool HasCheck(string name);

    /// <summary>Runs the named check, substituting <c>{key}</c> placeholders in its arguments with the
    /// supplied values. Returns a failed result (exit -1) when the check is unknown.</summary>
    Task<CheckResult> RunAsync(
        string checkName, IReadOnlyDictionary<string, string> arguments, CancellationToken cancellationToken = default);
}
