using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
namespace Agora.Orchestration.Concretes;

/// <summary>Thrown when graph execution fails at runtime (e.g. a dead-end with no outgoing edge).</summary>
public sealed class ExecutionError : Exception
{
    /// <summary>Creates the exception with an error message.</summary>
    public ExecutionError(string message) : base(message) { }
}
