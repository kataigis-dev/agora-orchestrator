using Agora.Providers;

namespace Agora.Eval;

/// <summary>Runs an eval <see cref="Scenario"/> against a config using a scripted fake provider,
/// then checks the expectations. Deterministic and offline — ideal for pipeline regression tests.</summary>
public static class ScenarioRunner
{
    /// <summary>Runs the scenario against the config with scripted responses and returns whether all
    /// output/signal expectations held.</summary>
    public static async Task<EvalResult> RunAsync(string configPath, Scenario scenario)
    {
        var provider = new FakeChatProvider(scenario.Responses);
        var runtime = Runtime.FromConfig(configPath, provider);

        string output;
        IReadOnlyDictionary<string, object> signals;
        if (scenario.Graph)
        {
            var result = await runtime.RunAsync(scenario.Input);
            output = result.Output;
            signals = result.State.Signals;
        }
        else
        {
            var agentId = scenario.Agent
                ?? throw new ArgumentException("scenario with graph=false must set 'agent'");
            var result = await runtime.RunAgentAsync(agentId, scenario.Input);
            output = result.Output;
            signals = result.Signals;
        }

        var failures = new List<string>();
        foreach (var expected in scenario.ExpectOutputContains)
            if (!output.Contains(expected, StringComparison.OrdinalIgnoreCase))
                failures.Add($"output did not contain '{expected}'");
        foreach (var signal in scenario.ExpectSignals)
            if (!signals.ContainsKey(signal))
                failures.Add($"missing expected signal '{signal}'");

        return new EvalResult(failures.Count == 0, failures);
    }
}
