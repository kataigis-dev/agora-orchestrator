namespace Agora.Agents;

public interface IAgent
{
    Task<AgentResult> RunAsync(string userInput, string context = "");
}
