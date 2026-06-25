using Agora.Agents.Contracts;
using Agora.Configuration;

namespace Agora.Communication;

/// <summary>
/// Owns how an agent is instructed to communicate and how its output is read back. The communication
/// protocol (natural vs H2C) pairs a system-prompt preamble with an <see cref="IOutputInterpreter"/>, and
/// the handoff and output-language directives layer on top in a fixed order. One module assembles the whole
/// instruction prefix and selects the interpreter, so changing a protocol or directive is a single place
/// (and the preamble can never drift out of sync with the interpreter that parses its replies).
/// </summary>
public sealed class AgentInstructions
{
    private readonly bool _h2c;
    private readonly bool _handoff;
    private readonly string? _language;

    private AgentInstructions(bool h2c, bool handoff, string? language)
    {
        _h2c = h2c;
        _handoff = handoff;
        _language = language;
        Interpreter = h2c ? new H2cInterpreter() : new SignalInterpreter();
    }

    /// <summary>Builds the instruction set from config: communication mode, handoff, and output language.</summary>
    public static AgentInstructions For(AgoraConfig config) => new(
        string.Equals(config.Communication, "h2c", StringComparison.OrdinalIgnoreCase),
        config.Handoff == true,
        config.Language);

    /// <summary>The output interpreter for the configured protocol — stable across the run.</summary>
    public IOutputInterpreter Interpreter { get; }

    /// <summary>The prefix to prepend to an agent's own instructions: the output-language directive, then
    /// the protocol preamble, then the handoff directive — skipped in <paramref name="answerMode"/>, where
    /// the agent is answering an <c>ask_agent</c> question rather than producing a handoff. Empty when no
    /// directive applies.</summary>
    public string Prefix(bool answerMode)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_language))
            parts.Add(LanguageDirective(_language));
        if (_h2c)
            parts.Add(H2cProtocol);
        if (_handoff && !answerMode)
            parts.Add(HandoffDirective(_h2c));
        return string.Join("\n\n", parts);
    }

    // The H2C protocol preamble: instructs the agent to reply in token-compressed blocks that H2cInterpreter parses.
    private const string H2cProtocol =
        "COMMUNICATION PROTOCOL: H2C (token-compressed). Reply using H2C blocks only — no prose.\n" +
        "Block syntax: a header line [TYPE:SUBTYPE] optionally followed by one fields line key:value|key:value.\n" +
        "TYPES: ARCH BUILD TEST CTX STATE ORCH SKILL.\n" +
        "SUBTYPES: PLAN EXEC DONE FIX REVERT NACK RUN PASS FAIL PRIMITIVES UPDATE NEGOTIATE FINDINGS ACK END PROMPT.\n" +
        "Lists use [a,b,c]; file revisions use file~N. Signal completion/verdicts via the subtype " +
        "(e.g. [STATE:DONE], [TEST:PASS], [STATE:FIX]). Read incoming context as H2C blocks.\n" +
        "Example:\n[ARCH:PLAN]\nid:api-meteo|fw:net10|lib:[fastapi,httpx]";

    // Handoff directive, phrased for the active protocol (h2c field vs natural artifact token).
    private static string HandoffDirective(bool h2c) => h2c
        ? "HANDOFF MODE: the next agent does NOT see your full output. Pass only what it needs as a "
          + "`handoff:<short text>` field inside one block (e.g. [CTX:UPDATE]\\nhandoff:auth uses JWT)."
        : "HANDOFF MODE: the next agent does NOT see your full output. Pass only the essential context "
          + "it needs by emitting <<artifact handoff=...>> with everything required to continue.";

    // Output-language directive (config: language).
    private static string LanguageDirective(string language)
        => $"OUTPUT LANGUAGE: write all generated documents, artifacts, and responses in {language}.";
}
