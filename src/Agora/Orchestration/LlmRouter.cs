using Agora.Providers;

namespace Agora.Orchestration;

/// <summary>Asks an LLM to choose the next branch by matching the agent's output against each
/// branch's description. Falls back to the first option if the reply names no valid target.</summary>
public sealed class LlmRouter : IRouter
{
    private const string System =
        "You are a router. Given an agent's output and a list of labelled destinations, reply with "
        + "ONLY the single destination label that best fits. Output just the label, nothing else.";

    private readonly IChatProvider _provider;
    private readonly ModelSpec _spec;

    /// <summary>Creates the router backed by the given chat provider and model.</summary>
    public LlmRouter(IChatProvider provider, ModelSpec spec)
    {
        _provider = provider;
        _spec = spec;
    }

    /// <inheritdoc />
    public async Task<string> ChooseAsync(
        string context, IReadOnlyList<RouteOption> options, CancellationToken cancellationToken = default)
    {
        if (options.Count == 1)
            return options[0].Target;

        var list = string.Join("\n", options.Select(o => $"- {o.Target}: {o.Description}"));
        var messages = new[]
        {
            new ChatMessage("system", System),
            new ChatMessage("user", $"OUTPUT:\n{context}\n\nDESTINATIONS:\n{list}\n\nLabel:"),
        };
        var result = await _provider.CompleteAsync(messages, _spec, cancellationToken);
        var reply = result.Text.Trim();

        // Prefer an exact label, else the first label mentioned anywhere in the reply.
        var exact = options.FirstOrDefault(o => string.Equals(o.Target, reply, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact.Target;
        var mentioned = options.FirstOrDefault(o => reply.Contains(o.Target, StringComparison.OrdinalIgnoreCase));
        return mentioned?.Target ?? options[0].Target;
    }
}
