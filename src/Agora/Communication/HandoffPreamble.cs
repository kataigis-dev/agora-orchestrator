namespace Agora.Communication;

/// <summary>System-prompt preamble used when handoff mode is on: instructs an agent to
/// pass only the essential context the next agent needs, not its full output.</summary>
public static class HandoffPreamble
{
    /// <summary>Returns the handoff preamble phrased for H2C or natural communication mode.</summary>
    public static string For(bool h2c) => h2c
        ? "HANDOFF MODE: the next agent does NOT see your full output. Pass only what it needs as a "
          + "`handoff:<short text>` field inside one block (e.g. [CTX:UPDATE]\\nhandoff:auth uses JWT)."
        : "HANDOFF MODE: the next agent does NOT see your full output. Pass only the essential context "
          + "it needs by emitting <<artifact handoff=...>> with everything required to continue.";
}
