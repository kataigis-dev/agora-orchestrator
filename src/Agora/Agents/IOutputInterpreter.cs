namespace Agora.Agents;

/// <summary>Turns an agent's raw text output into a (display output, control signals) pair.</summary>
public interface IOutputInterpreter
{
    (string Output, Dictionary<string, object> Signals) Interpret(string text);
}

/// <summary>Natural-language mode: extracts and strips <c>&lt;&lt;signal&gt;&gt;</c> tokens.</summary>
public sealed class SignalInterpreter : IOutputInterpreter
{
    public (string Output, Dictionary<string, object> Signals) Interpret(string text)
        => SignalParser.Extract(text);
}
