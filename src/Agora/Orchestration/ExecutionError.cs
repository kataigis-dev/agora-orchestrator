namespace Agora.Orchestration;

public sealed class ExecutionError : Exception
{
    public ExecutionError(string message) : base(message) { }
}
