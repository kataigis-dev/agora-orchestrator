using Agora.Verification.Contracts;
using Agora.Verification.Models;
using Agora.Verification.Concretes;
namespace Agora.Verification.Models;

/// <summary>The deterministic outcome of running a configured check: whether it passed (exit code 0),
/// the exit code, a captured output excerpt, and how long it took. This is what replaces an
/// LLM-asserted "tests pass" with the real result of a build/test command.</summary>
public sealed record CheckResult(string Name, bool Passed, int ExitCode, string Output, long DurationMs)
{
    /// <summary>A one-line summary (no output body) for signals and logs.</summary>
    public string ToSummary() => $"{Name}: {(Passed ? "PASS" : "FAIL")} (exit {ExitCode}, {DurationMs}ms)";
}
