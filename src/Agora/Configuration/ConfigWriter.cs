using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Agora.Configuration;

/// <summary>
/// Serializes an <see cref="AgoraConfig"/> back to YAML. Counterpart to
/// <see cref="ConfigLoader"/>; used by the guided <c>init</c> command.
/// </summary>
public static class ConfigWriter
{
    /// <summary>Serializes the config to a YAML string, omitting null and empty sections.</summary>
    public static string ToYaml(AgoraConfig config)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(
                DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
            .Build();
        return serializer.Serialize(config);
    }

    /// <summary>Serializes the config and writes it to <paramref name="path"/>.</summary>
    public static void Save(AgoraConfig config, string path)
        => File.WriteAllText(path, ToYaml(config));
}
