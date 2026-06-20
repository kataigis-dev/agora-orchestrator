namespace Agora.Orchestration;

/// <summary>Thrown when a graph definition is missing, malformed, or fails validation.</summary>
public sealed class GraphError : Exception
{
    /// <summary>Creates the exception with an error message.</summary>
    public GraphError(string message) : base(message) { }
}
