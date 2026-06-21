using Agora.Specs;
using Agora.Verification;

namespace Agora.Tests.Verification;

public class AcceptanceVerifierTests
{
    /// <summary>A check runner whose verdicts are scripted by check name.</summary>
    private sealed class FakeRunner : ICheckRunner
    {
        private readonly Dictionary<string, bool> _outcomes;
        public string Workdir { get; }
        public FakeRunner(Dictionary<string, bool> outcomes, string workdir = "")
        {
            _outcomes = outcomes;
            Workdir = workdir.Length == 0 ? Directory.GetCurrentDirectory() : workdir;
        }
        public bool HasCheck(string name) => _outcomes.ContainsKey(name);
        public Task<CheckResult> RunAsync(string name, IReadOnlyDictionary<string, string> args, CancellationToken ct = default)
            => Task.FromResult(_outcomes.TryGetValue(name, out var pass)
                ? new CheckResult(name, pass, pass ? 0 : 1, "", 1)
                : new CheckResult(name, false, -1, "unknown", 0));
    }

    private static Requirement Req(params AcceptanceCriterion[] criteria) =>
        new() { Id = "R1", Title = "r", AcceptanceCriteria = criteria };

    [Fact]
    public async Task AllAutomatedPassing_VerifiesDeterministically()
    {
        var req = Req(
            new AcceptanceCriterion("R1.A1", "builds", new SpecCheck(CheckKind.Command, "build")),
            new AcceptanceCriterion("R1.A2", "tested", new SpecCheck(CheckKind.Test, "test filter=Auth")));
        var verifier = new AcceptanceVerifier(new FakeRunner(new() { ["build"] = true, ["test"] = true }));

        var result = await verifier.VerifyAsync(req);

        Assert.True(result.Verified);
        Assert.True(result.AutomatedPassed);
        Assert.All(result.Criteria, c => Assert.True(c.Passed));
    }

    [Fact]
    public async Task OneFailingCheck_BlocksVerification()
    {
        var req = Req(
            new AcceptanceCriterion("R1.A1", "builds", new SpecCheck(CheckKind.Command, "build")),
            new AcceptanceCriterion("R1.A2", "tested", new SpecCheck(CheckKind.Test, "test")));
        var verifier = new AcceptanceVerifier(new FakeRunner(new() { ["build"] = true, ["test"] = false }));

        var result = await verifier.VerifyAsync(req);

        Assert.False(result.Verified);
        Assert.Contains(result.Criteria, c => c.CriterionId == "R1.A2" && !c.Passed);
    }

    [Fact]
    public async Task ManualCriterion_NeedsSignOff_NotAutoVerified()
    {
        var req = Req(
            new AcceptanceCriterion("R1.A1", "builds", new SpecCheck(CheckKind.Command, "build")),
            new AcceptanceCriterion("R1.A2", "looks good", new SpecCheck(CheckKind.Manual, "")));
        var verifier = new AcceptanceVerifier(new FakeRunner(new() { ["build"] = true }));

        var result = await verifier.VerifyAsync(req);

        Assert.True(result.AutomatedPassed);
        Assert.True(result.HasManual);
        Assert.False(result.Verified); // manual sign-off still pending
    }

    [Fact]
    public async Task FileExists_ChecksThePath()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"agora-fe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "present.txt"), "x");
            var verifier = new AcceptanceVerifier(new FakeRunner(new(), dir));

            var present = await verifier.VerifyAsync(Req(
                new AcceptanceCriterion("R1.A1", "file", new SpecCheck(CheckKind.FileExists, "present.txt"))));
            var missing = await verifier.VerifyAsync(Req(
                new AcceptanceCriterion("R1.A1", "file", new SpecCheck(CheckKind.FileExists, "absent.txt"))));

            Assert.True(present.Verified);
            Assert.False(missing.Verified);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
