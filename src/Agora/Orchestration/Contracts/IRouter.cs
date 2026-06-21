using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
namespace Agora.Orchestration.Contracts;

/// <summary>One branch an <see cref="IRouter"/> may choose: where it leads and when to take it.</summary>
public sealed record RouteOption(string Target, string Description);

/// <summary>
/// Picks the next node among <c>route</c> edges based on an agent's output and each branch's
/// description — a more robust alternative to brittle signal-token conditional routing.
/// </summary>
public interface IRouter
{
    /// <summary>Chooses one option's <see cref="RouteOption.Target"/> given the routing context.</summary>
    Task<string> ChooseAsync(
        string context, IReadOnlyList<RouteOption> options, CancellationToken cancellationToken = default);
}
