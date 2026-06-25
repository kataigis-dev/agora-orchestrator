using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Agora.Providers.Models;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework.Providers;

/// <summary>
/// A hand-rolled <see cref="IChatClient"/> for Anthropic's native Messages API. Unlike the
/// OpenAI-compatible path, it serializes per-block <c>cache_control</c> breakpoints to the wire (so the
/// <see cref="ChatMessage.CacheStable"/> hint actually takes effect) and reads Anthropic's
/// <c>cache_creation_input_tokens</c> / <c>cache_read_input_tokens</c> back into usage. Text completions
/// only — tool/function calling is not wired here (use the OpenAI-compatible path for tool-heavy agents).
/// </summary>
internal sealed class AnthropicChatClient : IChatClient
{
    private const string DefaultEndpoint = "https://api.anthropic.com/v1";
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient _http;
    private readonly string _model;
    private readonly Uri _messagesUri;

    private AnthropicChatClient(ModelSpec spec)
    {
        _model = spec.Model;
        var baseUrl = (spec.ApiBase ?? DefaultEndpoint).TrimEnd('/');
        _messagesUri = new Uri(baseUrl + "/messages");
        _http = new HttpClient
        {
            // Honor the configured timeout so the client and RetryPolicy agree (avoids the F13 trap of a
            // 100s default firing before the configured timeout).
            Timeout = TimeSpan.FromSeconds(spec.Timeout > 0 ? spec.Timeout : 120),
        };
        _http.DefaultRequestHeaders.Add("x-api-key", spec.ApiKey
            ?? throw new InvalidOperationException(
                "anthropic provider needs an API key; set the provider's api_key_env."));
        _http.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
    }

    /// <summary>Builds an Anthropic chat client for the spec.</summary>
    public static IChatClient Build(ModelSpec spec) => new AnthropicChatClient(spec);

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var body = BuildRequestJson(messages, options, _model);
        using var request = new HttpRequestMessage(HttpMethod.Post, _messagesUri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Anthropic API {(int)response.StatusCode}: {payload}");
        return ParseResponse(payload, _model);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Non-streaming under the hood (the eval path does not stream); emit one update carrying the text
        // and usage so the streaming consumer aggregates them correctly.
        var response = await GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        var contents = new List<AIContent> { new TextContent(response.Text) };
        if (response.Usage is not null)
            contents.Add(new UsageContent(response.Usage));
        yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = contents, ModelId = response.ModelId };
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();

    /// <summary>Serializes MEAI messages into an Anthropic Messages request, lifting system messages to the
    /// top-level <c>system</c> field and attaching <c>cache_control</c> to any cache-marked block.</summary>
    internal static string BuildRequestJson(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options, string model)
    {
        var systemBlocks = new JsonArray();
        var conversation = new JsonArray();
        foreach (var message in messages)
        {
            var marked = message.AdditionalProperties?.ContainsKey("cache_control") == true;
            if (message.Role == ChatRole.System)
            {
                systemBlocks.Add(TextBlock(message.Text ?? "", marked));
            }
            else
            {
                conversation.Add(new JsonObject
                {
                    ["role"] = message.Role == ChatRole.Assistant ? "assistant" : "user",
                    ["content"] = new JsonArray { TextBlock(message.Text ?? "", marked) },
                });
            }
        }

        var root = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = options?.MaxOutputTokens ?? 4096,
            ["messages"] = conversation,
        };
        if (options?.Temperature is float temperature)
            root["temperature"] = temperature;
        if (systemBlocks.Count > 0)
            root["system"] = systemBlocks;
        return root.ToJsonString();
    }

    /// <summary>Parses an Anthropic Messages response into a <see cref="ChatResponse"/>, mapping cache
    /// creation/read tokens into usage. <c>InputTokenCount</c> is the total prompt processed (fresh +
    /// cache-read + cache-write) so cache-read remains a subset, matching the OpenAI semantics.</summary>
    internal static ChatResponse ParseResponse(string body, string fallbackModel)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var text = new StringBuilder();
        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            foreach (var block in content.EnumerateArray())
                if (block.TryGetProperty("type", out var type) && type.GetString() == "text"
                    && block.TryGetProperty("text", out var t))
                    text.Append(t.GetString());

        var usage = new UsageDetails();
        if (root.TryGetProperty("usage", out var u))
        {
            var input = GetLong(u, "input_tokens") ?? 0;
            var cacheRead = GetLong(u, "cache_read_input_tokens") ?? 0;
            var cacheWrite = GetLong(u, "cache_creation_input_tokens") ?? 0;
            usage.InputTokenCount = input + cacheRead + cacheWrite;
            usage.OutputTokenCount = GetLong(u, "output_tokens") ?? 0;
            usage.CachedInputTokenCount = cacheRead;
            if (cacheWrite > 0)
                usage.AdditionalCounts = new AdditionalPropertiesDictionary<long>
                {
                    ["cache_creation_input_tokens"] = cacheWrite,
                };
        }

        var modelId = root.TryGetProperty("model", out var m) ? m.GetString() ?? fallbackModel : fallbackModel;
        return new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, text.ToString()))
        {
            ModelId = modelId,
            Usage = usage,
        };
    }

    private static JsonObject TextBlock(string text, bool cacheMarked)
    {
        var block = new JsonObject { ["type"] = "text", ["text"] = text };
        if (cacheMarked)
            block["cache_control"] = new JsonObject { ["type"] = "ephemeral" };
        return block;
    }

    private static long? GetLong(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : null;
}
