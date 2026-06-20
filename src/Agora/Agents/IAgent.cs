namespace Agora.Agents;

public interface IAgent
{
    /// <summary>Runs the agent. When <paramref name="onChunk"/> is supplied and the backing
    /// provider supports streaming, response tokens are delivered to it as they arrive.</summary>
    Task<AgentResult> RunAsync(string userInput, string context = "", Action<string>? onChunk = null);
}
