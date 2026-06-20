using Agora.Configuration;

namespace Agora.Providers;

/// <summary>Flattens config models + providers + defaults into runnable <see cref="ModelSpec"/>s.</summary>
public static class ModelResolver
{
    /// <summary>Builds a model-spec per alias, resolving each provider's API key (from its env var)
    /// and base URL and folding in the default runtime settings.</summary>
    public static Dictionary<string, ModelSpec> Resolve(AgoraConfig config)
    {
        var specs = new Dictionary<string, ModelSpec>();
        var d = config.Defaults;
        foreach (var (alias, model) in config.Models)
        {
            string? apiKey = null;
            string? apiBase = null;
            if (config.Providers.TryGetValue(model.Provider, out var providerCfg))
            {
                if (!string.IsNullOrEmpty(providerCfg.ApiKeyEnv))
                    apiKey = Environment.GetEnvironmentVariable(providerCfg.ApiKeyEnv);
                apiBase = providerCfg.BaseUrl;
            }
            specs[alias] = new ModelSpec
            {
                Alias = alias, Provider = model.Provider, Model = model.Model,
                Temperature = d.Temperature, MaxTokens = d.MaxTokens,
                Timeout = d.Timeout, Retries = d.Retries, RetryBaseDelay = d.RetryBaseDelay,
                ApiKey = apiKey, ApiBase = apiBase,
            };
        }
        return specs;
    }
}
