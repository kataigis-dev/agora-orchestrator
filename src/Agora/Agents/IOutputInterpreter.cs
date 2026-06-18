namespace Agora.Agents;

/// <summary>Turns an agent's raw text output into display text, control signals, and artifacts.</summary>
public interface IOutputInterpreter
{
    (string Output, Dictionary<string, object> Signals, Dictionary<string, string> Artifacts) Interpret(string text);
}

/// <summary>Natural-language mode: extracts and strips <c>&lt;&lt;signal&gt;&gt;</c> and <c>&lt;&lt;artifact&gt;&gt;</c> tokens.</summary>
public sealed class SignalInterpreter : IOutputInterpreter
{
    public (string Output, Dictionary<string, object> Signals, Dictionary<string, string> Artifacts) Interpret(string text)
        => SignalParser.Extract(text);
}
