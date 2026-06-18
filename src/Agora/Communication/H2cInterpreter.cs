using Agora.Agents;

namespace Agora.Communication;

/// <summary>
/// H2C mode: derives routing signals from H2C blocks (each subtype → a true signal; each field → a
/// signal) and passes the H2C text through unchanged so downstream agents receive the blocks.
/// </summary>
public sealed class H2cInterpreter : IOutputInterpreter
{
    private readonly H2cParser _parser = new();

    public (string Output, Dictionary<string, object> Signals, Dictionary<string, string> Artifacts) Interpret(string text)
    {
        var signals = new Dictionary<string, object>();
        foreach (var block in _parser.Parse(text))
        {
            signals[block.Subtype.ToLowerInvariant()] = true;
            foreach (var (key, value) in block.Fields)
                signals[key] = value;
        }
        return (text, signals, new Dictionary<string, string>());
    }
}
