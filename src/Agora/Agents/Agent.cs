using Agora.Providers;

namespace Agora.Agents;

/// <summary>Runs a single agent's prompt loop (no tools/skills — see IToolAgentFactory for those).</summary>
public sealed class Agent : IAgent
{
    private readonly AgentCard _card;
    private readonly IChatProvider _provider;
    private readonly ModelSpec _spec;
    private readonly IOutputInterpreter _interpreter;

    /// <summary>Creates an agent bound to a chat provider, model spec, and output interpreter
    /// (defaults to natural-language signal parsing when none is supplied).</summary>
    public Agent(AgentCard card, IChatProvider provider, ModelSpec spec, IOutputInterpreter? interpreter = null)
    {
        _card = card;
        _provider = provider;
        _spec = spec;
        _interpreter = interpreter ?? new SignalInterpreter();
    }

    /// <summary>Builds the prompt (system + optional context + user input), completes it (streaming
    /// when <paramref name="onChunk"/> and a streaming provider are available), and interprets the
    /// reply into output text, signals, and artifacts.</summary>
    public async Task<AgentResult> RunAsync(string userInput, string context = "", Action<string>? onChunk = null)
    {
        var messages = new List<ChatMessage>();
        var system = _card.ComposeInstructions();
        // The system prompt is stable across a run, so mark it as a cacheable prefix (see PromptCaching);
        // providers that don't cache it simply ignore the hint.
        if (!string.IsNullOrEmpty(system))
            messages.Add(new ChatMessage("system", system, CacheStable: true));
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
            CacheReadTokens = result.CacheReadTokens,
            CacheWriteTokens = result.CacheWriteTokens,
            Signals = signals,
            Artifacts = artifacts,
        };
    }
}
