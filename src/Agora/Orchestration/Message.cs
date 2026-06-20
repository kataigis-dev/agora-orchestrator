namespace Agora.Orchestration;

/// <summary>A message exchanged between agents on the shared blackboard.</summary>
/// <param name="Sender">Id of the sending agent.</param>
/// <param name="Recipient">Id of the receiving agent.</param>
/// <param name="Content">Message body.</param>
public sealed record Message(string Sender, string Recipient, string Content);
