using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Agora.Configuration;

/// <summary>Loads and validates an <see cref="AgoraConfig"/> from a YAML file.</summary>
public static class ConfigLoader
{
    /// <summary>Reads, deserializes, and validates the config at <paramref name="path"/>.</summary>
    /// <exception cref="ConfigException">If the file is missing, the YAML is invalid, or references don't resolve.</exception>
    public static AgoraConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new ConfigException($"config file not found: {path}");

        AgoraConfig config;
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            config = deserializer.Deserialize<AgoraConfig>(File.ReadAllText(path)) ?? new AgoraConfig();
        }
        catch (YamlException e)
        {
            throw new ConfigException($"invalid YAML in {path}: {e.Message}", e);
        }

        ValidateReferences(config);
        return config;
    }

    /// <summary>Checks the communication mode and that every agent resolves a valid model,
    /// provider, and approvals subset.</summary>
    private static void ValidateReferences(AgoraConfig config)
    {
        if (config.Communication is not ("h2c" or "natural"))
            throw new ConfigException($"communication must be 'h2c' or 'natural', got '{config.Communication}'");

        if (config.Agents.Count == 0)
            throw new ConfigException("config must define at least one agent");

        foreach (var (agentId, agent) in config.Agents)
        {
            var alias = agent.Model ?? config.Defaults.Model;
            if (alias is null)
                throw new ConfigException($"agent '{agentId}' has no model and no defaults.model");
            if (!config.Models.TryGetValue(alias, out var model))
                throw new ConfigException($"agent '{agentId}' references unknown model alias '{alias}'");
            if (config.Providers.Count > 0 && !config.Providers.ContainsKey(model.Provider))
                throw new ConfigException($"model '{alias}' references unknown provider '{model.Provider}'");

            foreach (var approval in agent.Approvals)
                if (!agent.Tools.Contains(approval))
                    throw new ConfigException(
                        $"agent '{agentId}' approval '{approval}' is not in its tools list");
        }
    }
}
