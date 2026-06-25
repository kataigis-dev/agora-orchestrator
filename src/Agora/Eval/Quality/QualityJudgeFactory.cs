using Agora.Configuration;
using Agora.Providers.Concretes;
using Agora.Providers.Contracts;

namespace Agora.Eval.Quality;

/// <summary>Builds an <see cref="LlmJudge"/> from a judge config: it resolves the judge model spec (forcing
/// temperature 0) and drives it through the same injected provider as the system-under-test, so no extra
/// provider wiring is needed. The judge model should differ from the SUT model (no self-judging).</summary>
public static class QualityJudgeFactory
{
    /// <summary>Loads <paramref name="judgeConfigPath"/>, resolves its <c>defaults.model</c> spec, and
    /// returns a fail-closed JSON judge over <paramref name="provider"/>.</summary>
    public static IJudge FromConfig(string judgeConfigPath, IChatProvider provider)
    {
        var config = ConfigLoader.Load(judgeConfigPath);
        var specs = ModelResolver.Resolve(config);
        var alias = config.Defaults.Model
            ?? throw new ConfigException("judge config must set 'defaults.model'");
        if (!specs.TryGetValue(alias, out var spec))
            throw new ConfigException($"judge model alias '{alias}' not found in judge config");
        return new LlmJudge(provider, spec);
    }
}
