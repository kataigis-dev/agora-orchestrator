using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;

namespace Agora.Cli;

/// <summary>Parsed CLI options bundled with the injected I/O streams and edge dependencies
/// (provider, the agent backend, and HITL handlers) that each command needs.</summary>
internal record ConfigState(Dictionary<string, string> Options, IChatProvider? Provider, IAgentBackend? Backend, IApprovalHandler? ApprovalHandler,
    IConflictResolver? ConflictResolver,
    TextReader In, TextWriter Out, TextWriter Error);