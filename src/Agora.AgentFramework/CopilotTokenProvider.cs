using System.Net.Http.Headers;
using System.Text.Json;

namespace Agora.AgentFramework;

/// <summary>
/// Exchanges a long-lived GitHub OAuth token for the short-lived Copilot session token used to call
/// <c>api.githubcopilot.com</c>, caching it until shortly before it expires and refreshing on demand.
/// Safe for concurrent use: a single in-flight exchange is shared via the gate.
/// </summary>
internal sealed class CopilotTokenProvider
{
    private static readonly Uri TokenEndpoint = new("https://api.github.com/copilot_internal/v2/token");
    private static readonly HttpClient Http = new();

    private readonly string _oauthToken;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    /// <summary>Creates a provider that exchanges the given GitHub OAuth token.</summary>
    public CopilotTokenProvider(string oauthToken) => _oauthToken = oauthToken;

    /// <summary>Returns a valid Copilot session token, refreshing it if missing or near expiry.</summary>
    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (IsValid) return _token!;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (IsValid) return _token!;
            using var request = new HttpRequestMessage(HttpMethod.Get, TokenEndpoint);
            request.Headers.TryAddWithoutValidation("Authorization", $"token {_oauthToken}");
            request.Headers.TryAddWithoutValidation("Editor-Version", CopilotChatClient.EditorVersion);
            request.Headers.TryAddWithoutValidation("Editor-Plugin-Version", CopilotChatClient.PluginVersion);
            request.Headers.TryAddWithoutValidation("User-Agent", CopilotChatClient.UserAgent);

            using var response = await Http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"GitHub Copilot token exchange failed ({(int)response.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            _token = doc.RootElement.TryGetProperty("token", out var t) ? t.GetString() : null;
            if (string.IsNullOrEmpty(_token))
                throw new InvalidOperationException("GitHub Copilot token response had no 'token' field");
            var expiresAt = doc.RootElement.TryGetProperty("expires_at", out var e) ? e.GetInt64() : 0;
            _expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAt);
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>True when a token is cached and still has more than a two-minute safety margin.</summary>
    private bool IsValid => _token is not null && DateTimeOffset.UtcNow < _expiresAt - TimeSpan.FromMinutes(2);
}

/// <summary>
/// HTTP handler that stamps every Copilot request with the editor headers and a fresh session token
/// (from <see cref="CopilotTokenProvider"/>), overwriting whatever the OpenAI client set.
/// </summary>
internal sealed class CopilotAuthHandler : DelegatingHandler
{
    private readonly CopilotTokenProvider _tokens;

    /// <summary>Wraps a default inner handler, authenticating requests via the token provider.</summary>
    public CopilotAuthHandler(CopilotTokenProvider tokens)
    {
        _tokens = tokens;
        InnerHandler = new HttpClientHandler();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokens.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Remove("Editor-Version");
        request.Headers.Remove("Editor-Plugin-Version");
        request.Headers.Remove("Copilot-Integration-Id");
        request.Headers.TryAddWithoutValidation("Editor-Version", CopilotChatClient.EditorVersion);
        request.Headers.TryAddWithoutValidation("Editor-Plugin-Version", CopilotChatClient.PluginVersion);
        request.Headers.TryAddWithoutValidation("Copilot-Integration-Id", CopilotChatClient.IntegrationId);
        return await base.SendAsync(request, cancellationToken);
    }
}
