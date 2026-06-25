using Agora.Configuration;
using Agora.Specs.Concretes;
using Xunit;

namespace Agora.Tests.Specs;

public class SpecStoreFactoryTests
{
    private static AgoraConfig Config(string specSection)
    {
        var yaml =
            specSection +
            "providers: { openai: { api_key_env: K } }\n" +
            "models: { m: { provider: openai, model: x } }\n" +
            "agents: { a: { model: m, role: \"r\" } }\n";
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, yaml);
        try { return ConfigLoader.Load(path); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_SpecDisabled_ReturnsNull()
    {
        var config = Config("spec: { enabled: false }\n");
        Assert.Null(SpecStoreFactory.Build(config, Path.GetTempPath(), backend: null));
    }

    [Fact]
    public void Build_NoSpecSection_ReturnsNull()
    {
        var config = Config("");
        Assert.Null(SpecStoreFactory.Build(config, Path.GetTempPath(), backend: null));
    }

    [Fact]
    public void Build_FileStore_BuildsCoreStoreWithoutBackend()
    {
        var config = Config("spec: { enabled: true, store: { type: file } }\n");
        var store = SpecStoreFactory.Build(config, Path.GetTempPath(), backend: null);
        Assert.NotNull(store);   // the core file store needs no backend
    }
}
