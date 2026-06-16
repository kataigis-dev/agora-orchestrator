using Agora.Providers;

namespace Agora.Rag;

public sealed class LlmRefiner : IRefiner
{
    private const string SystemPrompt =
        "You rewrite a user's request into a clearer search query. " +
        "Reply with the improved query on the first line. " +
        "If helpful, add related sub-queries on following lines, one per line.";

    private readonly IChatProvider _provider;
    private readonly ModelSpec _spec;

    public LlmRefiner(IChatProvider provider, ModelSpec spec)
    {
        _provider = provider;
        _spec = spec;
    }

    public async Task<RefinedQuery> RefineAsync(string text, CancellationToken cancellationToken = default)
    {
        var messages = new[]
        {
            new ChatMessage("system", SystemPrompt),
            new ChatMessage("user", text),
        };
        var result = await _provider.CompleteAsync(messages, _spec, cancellationToken);
        var lines = result.Text
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
        if (lines.Count == 0)
            return new RefinedQuery { Query = text };
        return new RefinedQuery { Query = lines[0], SubQueries = lines.Skip(1).ToList() };
    }
}
