namespace Agora.Configuration;

/// <summary>One runnable check: an executable plus its argument tokens. Arguments may contain
/// <c>{key}</c> placeholders substituted at call time. The executable and arguments are passed
/// directly to the process (no shell), so values can never be interpreted as shell syntax.</summary>
public sealed class CheckCommandConfig
{
    /// <summary>Executable to run (e.g. <c>dotnet</c>).</summary>
    public string Command { get; set; } = "";

    /// <summary>Argument tokens (e.g. <c>[test, --filter, "{filter}"]</c>); each is one argv entry.</summary>
    public List<string> Args { get; set; } = new();
}

/// <summary>Real build/test execution configuration: a fixed allow-list of named commands that the
/// <c>run_check</c> tool and acceptance verification may invoke. Absent ⇒ no execution capability.</summary>
public sealed class ChecksConfig
{
    /// <summary>Directory the checks run in (relative to the config dir); defaults to the config dir.</summary>
    public string? Workdir { get; set; }

    /// <summary>Per-check timeout in seconds.</summary>
    public double Timeout { get; set; } = 120;

    /// <summary>The allow-listed checks, keyed by logical name (e.g. <c>build</c>, <c>test</c>).</summary>
    public Dictionary<string, CheckCommandConfig> Commands { get; set; } = new();
}
