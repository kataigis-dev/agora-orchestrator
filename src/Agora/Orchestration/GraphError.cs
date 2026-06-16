namespace Agora.Orchestration;

public sealed class GraphError : Exception
{
    public GraphError(string message) : base(message) { }
}
