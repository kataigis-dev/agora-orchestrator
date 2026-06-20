namespace Agora.Runs;

/// <summary>Lifecycle state of an API run.</summary>
public enum RunStatus
{
    /// <summary>The run is executing.</summary>
    Running,

    /// <summary>The run is paused waiting for a human approval decision.</summary>
    AwaitingApproval,

    /// <summary>The run finished successfully.</summary>
    Completed,

    /// <summary>The run ended with an error.</summary>
    Failed,
}
