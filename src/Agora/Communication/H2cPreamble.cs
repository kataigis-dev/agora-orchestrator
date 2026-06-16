namespace Agora.Communication;

/// <summary>System-prompt preamble instructing an agent to communicate in the H2C protocol.</summary>
public static class H2cPreamble
{
    public const string Text =
        "COMMUNICATION PROTOCOL: H2C (token-compressed). Reply using H2C blocks only — no prose.\n" +
        "Block syntax: a header line [TYPE:SUBTYPE] optionally followed by one fields line key:value|key:value.\n" +
        "TYPES: ARCH BUILD TEST CTX STATE ORCH SKILL.\n" +
        "SUBTYPES: PLAN EXEC DONE FIX REVERT NACK RUN PASS FAIL PRIMITIVES UPDATE NEGOTIATE FINDINGS ACK END PROMPT.\n" +
        "Lists use [a,b,c]; file revisions use file~N. Signal completion/verdicts via the subtype " +
        "(e.g. [STATE:DONE], [TEST:PASS], [STATE:FIX]). Read incoming context as H2C blocks.\n" +
        "Example:\n[ARCH:PLAN]\nid:api-meteo|fw:net10|lib:[fastapi,httpx]";
}
