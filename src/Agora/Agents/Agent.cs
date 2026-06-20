using Agora.Providers;

namespace Agora.Agents;

/// <summary>Runs a single agent's prompt loop (no tools/skills — see IToolAgentFactory for those).</summary>
public sealed class Agent : IAgent
{
    private readonly AgentCard _card;
    private readonly IChatProvider _provider;
    private readonly ModelSpec _spec;
    private readonly IOutputInterpreter _interpreter;

    public Agent(AgentCard card, IChatProvider provider, ModelSpec spec, IOutputInterpreter? interpreter = null)
    {
        _card = card;
        _provider = provider;
        _spec = spec;
        _interpreter = interpreter ?? new SignalInterpreter();
    }

    public async Task<AgentResult> RunAsync(string userInput, string context = "", Action<string>? onChunk = null)
    {
        var messages = new List<ChatMessage>();
        var system = BuildSystemPrompt();
        if (!string.IsNullOrEmpty(system))
            messages.Add(new ChatMessage("system", system));
        var userContent = string.IsNullOrEmpty(context) ? userInput : $"{context}\n\n{userInput}";
        messages.Add(new ChatMessage("user", userContent));

        var result = onChunk is not null && _provider is IStreamingChatProvider streaming
            ? await streaming.StreamAsync(messages, _spec, onChunk)
            : await _provider.CompleteAsync(messages, _spec);
        var (output, signals, artifacts) = _interpreter.Interpret(result.Text);
        return new AgentResult
        {
            Output = output,
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Signals = signals,
            Artifacts = artifacts,
        };
    }

    private string BuildSystemPrompt()
    {
        var parts = new[] { _card.Role, _card.SystemPrompt }.Where(p => !string.IsNullOrEmpty(p));
        return string.Join("\n\n", parts);
    }
}
