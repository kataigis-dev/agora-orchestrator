using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Agora.Providers;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Agora.AgentFramework;

/// <summary>
/// Builds an <see cref="IChatClient"/> for the GitHub Copilot editor endpoint
/// (<c>api.githubcopilot.com</c>). It speaks the OpenAI chat-completions wire format but requires the
/// editor headers and a short-lived session token, both injected by <see cref="CopilotAuthHandler"/>.
/// </summary>
internal static class CopilotChatClient
{
    /// <summary>Default Copilot chat-completions base URL (overridable via the provider's base_url).</summary>
    public const string DefaultEndpoint = "https://api.githubcopilot.com";

    /// <summary>Editor identity headers the Copilot backend expects on every request.</summary>
    public const string EditorVersion = "vscode/1.95.0";

    /// <inheritdoc cref="EditorVersion"/>
    public const string PluginVersion = "copilot-chat/0.23.0";

    /// <inheritdoc cref="EditorVersion"/>
    public const string IntegrationId = "vscode-chat";

    /// <inheritdoc cref="EditorVersion"/>
    public const string UserAgent = "GitHubCopilotChat/0.23.0";

    /// <summary>Creates a Copilot-backed chat client for the spec, resolving the OAuth token and wiring
    /// the session-token exchange and editor headers through a custom transport.</summary>
    public static IChatClient Build(ModelSpec spec)
    {
        var oauthToken = ResolveOAuthToken(spec.ApiKey);
        var tokens = new CopilotTokenProvider(oauthToken);
        var transport = new HttpClientPipelineTransport(new HttpClient(new CopilotAuthHandler(tokens)));
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(spec.ApiBase ?? DefaultEndpoint),
            Transport = transport,
        };
        // The Authorization header is replaced per-request by CopilotAuthHandler; this credential is a placeholder.
        var client = new OpenAIClient(new ApiKeyCredential(oauthToken), options);
        return client.GetChatClient(spec.Model).AsIChatClient();
    }

    /// <summary>Returns the GitHub OAuth token from the configured value, falling back to the editor's
    /// Copilot sign-in file (<c>~/.config/github-copilot/apps.json</c> or <c>hosts.json</c>).</summary>
    private static string ResolveOAuthToken(string? configured)
    {
        if (!string.IsNullOrEmpty(configured))
            return configured;
        if (TryReadEditorToken(out var token))
            return token;
        throw new InvalidOperationException(
            "github-copilot provider needs a GitHub OAuth token. Set the provider's api_key_env to one, "
            + "or sign in to Copilot in your editor so it appears in ~/.config/github-copilot/apps.json.");
    }

    /// <summary>Tries to read an <c>oauth_token</c> from the editor's Copilot config files.</summary>
    private static bool TryReadEditorToken(out string token)
    {
        token = string.Empty;
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(configHome))
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        var dir = Path.Combine(configHome, "github-copilot");

        foreach (var file in new[] { "apps.json", "hosts.json" })
        {
            var path = Path.Combine(dir, file);
            if (!File.Exists(path)) continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                foreach (var entry in doc.RootElement.EnumerateObject())
                    if (entry.Value.ValueKind == JsonValueKind.Object
                        && entry.Value.TryGetProperty("oauth_token", out var t)
                        && t.GetString() is { Length: > 0 } value)
                    {
                        token = value;
                        return true;
                    }
            }
            catch (JsonException)
            {
                // Ignore a malformed file and try the next one.
            }
        }
        return false;
    }
}
