using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
namespace Agora.Orchestration.Concretes;

/// <summary>Default <see cref="IExecutionObserver"/>: renders the run as colored console output
/// (the rendering formerly inlined in <see cref="GraphExecutor"/>, extracted so the executor stays
/// presentation-free). Long inputs/artifacts are truncated for display only.</summary>
public sealed class ConsoleExecutionObserver : IExecutionObserver
{
    private const int InputPreview = 50;
    private const int ArtifactPreview = 60;

    /// <inheritdoc />
    public void OnGraphStart(Graph graph)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Line("━━━ Agent Graph Execution ━━━");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Part("  Graph: ");
        Console.ForegroundColor = ConsoleColor.White;
        var lines = graph.Edges.Select(e => $"{e.Source} ──{e.Type}→ {e.Target}");
        Line(string.Join("\n" + new string(' ', 9), lines));
        Line("");
        Console.ForegroundColor = original;
    }

    /// <inheritdoc />
    public void OnNodeStart(int step, string nodeId, string input)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Part($"  ▶ [{step}] Agent: ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Line(nodeId);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Line($"  └─ input: {Truncate(input, InputPreview)}");
        Line("");
        Console.ForegroundColor = original;
    }

    /// <inheritdoc />
    public void OnParallel(IReadOnlyList<string> branches)
    {
        Line($"  ⇉ parallel: {string.Join(", ", branches)}");
        Line("");
    }

    /// <inheritdoc />
    public void OnSignals(IReadOnlyCollection<string> signals)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Line($"  └─ signals: {(signals.Count > 0 ? string.Join(", ", signals) : "none")}");
        Console.ForegroundColor = original;
    }

    /// <inheritdoc />
    public void OnUsage(TokenUsage usage)
    {
        if (usage.Input == 0 && usage.Output == 0)
            return;
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        var cache = usage.CacheRead > 0 ? $", {usage.CacheRead} cached" : "";
        Line($"  └─ tokens: {usage.Input} in / {usage.Output} out{cache}");
        Console.ForegroundColor = original;
    }

    /// <inheritdoc />
    public void OnArtifact(string key, string value)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Line($"  └─ artifact {key}: {Truncate(value, ArtifactPreview)}");
        Console.ForegroundColor = original;
    }

    /// <inheritdoc />
    public void OnEdge(string label, string target, bool isEnd)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Part($"  ──▶ {label} → ");
        Console.ForegroundColor = isEnd ? ConsoleColor.Magenta : ConsoleColor.White;
        Line(isEnd ? "END" : target);
        Line("");
        Console.ForegroundColor = original;
    }

    /// <inheritdoc />
    public void OnGraphComplete()
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Line("━━━ Execution Complete ━━━");
        Line("");
        Console.ForegroundColor = original;
    }

    private static void Line(string s) { Console.Out.WriteLine(s); Console.Out.Flush(); }
    private static void Part(string s) { Console.Out.Write(s); Console.Out.Flush(); }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";
}
