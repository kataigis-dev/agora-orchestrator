using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
namespace Agora.Orchestration.Concretes;

/// <summary>Thrown when a graph definition is missing, malformed, or fails validation.</summary>
public sealed class GraphError : Exception
{
    /// <summary>Creates the exception with an error message.</summary>
    public GraphError(string message) : base(message) { }
}
