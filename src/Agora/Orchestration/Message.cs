namespace Agora.Orchestration;

public sealed record Message(string Sender, string Recipient, string Content);
