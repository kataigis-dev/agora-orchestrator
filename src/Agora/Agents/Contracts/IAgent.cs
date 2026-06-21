using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
namespace Agora.Agents.Contracts;

/// <summary>A runnable agent: takes user input plus optional context and produces an <see cref="AgentResult"/>.</summary>
public interface IAgent
{
    /// <summary>Runs the agent. When <paramref name="onChunk"/> is supplied and the backing
    /// provider supports streaming, response tokens are delivered to it as they arrive.</summary>
    Task<AgentResult> RunAsync(string userInput, string context = "", Action<string>? onChunk = null);
}
