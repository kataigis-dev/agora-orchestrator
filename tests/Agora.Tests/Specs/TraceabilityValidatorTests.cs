using Agora.Specs;

namespace Agora.Tests.Specs;

public class TraceabilityValidatorTests
{
    private static Requirement Req(
        string id, RequirementStatus status, CheckKind checkKind = CheckKind.Test, string expression = "build")
        => new()
        {
            Id = id,
            Title = $"req {id}",
            Status = status,
            AcceptanceCriteria = new[] { new AcceptanceCriterion($"{id}.A1", "it works", new SpecCheck(checkKind, expression)) },
        };

    private static TaskItem Task(string id, params string[] reqs)
        => new() { Id = id, Description = $"do {id}", RequirementIds = reqs };

    [Fact]
    public void EmptySpec_IsIncomplete_NoApprovedRequirements()
    {
        var report = TraceabilityValidator.Analyze(SpecDocument.Empty);

        Assert.False(report.IsComplete);
        Assert.Contains(report.Gaps, g => g.Code == "no-approved-requirements" && g.Severity == SpecSeverity.Error);
    }

    [Fact]
    public void ProposedOnly_IsNotInScope_AndIncomplete()
    {
        var doc = SpecDocument.Empty.WithRequirement(Req("R1", RequirementStatus.Proposed));
        var report = TraceabilityValidator.Analyze(doc);

        Assert.Equal(0, report.InScopeCount);
        Assert.False(report.IsComplete);
    }

    [Fact]
    public void FullyVerifiedAndCovered_IsComplete()
    {
        var doc = SpecDocument.Empty
            .WithRequirement(Req("R1", RequirementStatus.Verified))
            .WithTask(Task("T1", "R1"));

        var report = TraceabilityValidator.Analyze(doc);

        Assert.True(report.IsComplete);
        Assert.Equal(1, report.InScopeCount);
        Assert.Equal(1, report.CoveredCount);
        Assert.Equal(1, report.VerifiedCount);
        Assert.Equal(1.0, report.CoverageRate, 3);
        Assert.Equal(1.0, report.VerificationRate, 3);
    }

    [Fact]
    public void ApprovedButUncovered_BlocksCompletion()
    {
        var doc = SpecDocument.Empty.WithRequirement(Req("R1", RequirementStatus.Approved));
        var report = TraceabilityValidator.Analyze(doc);

        Assert.False(report.IsComplete);
        Assert.Contains(report.Gaps, g => g.Code == "uncovered-requirement" && g.Subject == "R1");
    }

    [Fact]
    public void ImplementedButNotVerified_BlocksCompletion()
    {
        var doc = SpecDocument.Empty
            .WithRequirement(Req("R1", RequirementStatus.Implemented))
            .WithTask(Task("T1", "R1"));

        var report = TraceabilityValidator.Analyze(doc);

        Assert.False(report.IsComplete);
        Assert.Contains(report.Gaps, g => g.Code == "unverified-requirement" && g.Subject == "R1");
    }

    [Fact]
    public void VerifiedWithManualCriterion_IsUnsubstantiated()
    {
        // A requirement flipped to Verified by hand (spec_set_status) but whose acceptance is manual
        // could not have come from a deterministic check — the gate must reject it.
        var doc = SpecDocument.Empty
            .WithRequirement(Req("R1", RequirementStatus.Verified, CheckKind.Manual, expression: ""))
            .WithTask(Task("T1", "R1"));

        var report = TraceabilityValidator.Analyze(doc);

        Assert.False(report.IsComplete);
        Assert.Contains(report.Gaps, g => g.Code == "unsubstantiated-verification" && g.Subject == "R1");
        Assert.False(report.Rows.Single().VerificationSubstantiated);
    }

    [Fact]
    public void RejectedRequirement_IsOutOfScope_AndDoesNotBlock()
    {
        var doc = SpecDocument.Empty
            .WithRequirement(Req("R1", RequirementStatus.Verified))
            .WithTask(Task("T1", "R1"))
            .WithRequirement(Req("R2", RequirementStatus.Rejected));

        var report = TraceabilityValidator.Analyze(doc);

        Assert.True(report.IsComplete);            // R2 is rejected → not in scope
        Assert.Equal(1, report.InScopeCount);
    }

    [Fact]
    public void ReportText_StatesVerdict()
    {
        var doc = SpecDocument.Empty
            .WithRequirement(Req("R1", RequirementStatus.Approved))
            .WithTask(Task("T1", "R1"));

        var text = TraceabilityValidator.Analyze(doc).ToReport();

        Assert.Contains("INCOMPLETE", text);
        Assert.Contains("R1", text);
    }
}
