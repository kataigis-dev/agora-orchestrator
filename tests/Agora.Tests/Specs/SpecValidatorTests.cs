using Agora.Specs;

namespace Agora.Tests.Specs;

public class SpecValidatorTests
{
    private static Requirement Req(string id, RequirementStatus status = RequirementStatus.Proposed, bool withCriteria = true) => new()
    {
        Id = id,
        Title = $"req {id}",
        Status = status,
        AcceptanceCriteria = withCriteria
            ? new[] { new AcceptanceCriterion($"{id}.A1", "it works", new SpecCheck()) }
            : Array.Empty<AcceptanceCriterion>(),
    };

    [Fact]
    public void ValidDocument_HasNoErrors()
    {
        var doc = SpecDocument.Empty
            .WithRequirement(Req("R1"))
            .WithTask(new TaskItem { Id = "T1", Description = "do it", RequirementIds = new[] { "R1" } });

        Assert.True(SpecValidator.IsValid(doc));
    }

    [Fact]
    public void MissingCriteria_IsErrorWhenRequired_WarningOtherwise()
    {
        var doc = SpecDocument.Empty.WithRequirement(Req("R1", withCriteria: false));

        Assert.False(SpecValidator.IsValid(doc, requireCriteria: true));
        Assert.True(SpecValidator.IsValid(doc, requireCriteria: false));
        Assert.Contains(SpecValidator.Validate(doc, requireCriteria: false),
            i => i.Code == "missing-acceptance-criteria" && i.Severity == SpecSeverity.Warning);
    }

    [Fact]
    public void TaskReferencingUnknownRequirement_IsError()
    {
        var doc = SpecDocument.Empty
            .WithRequirement(Req("R1"))
            .WithTask(new TaskItem { Id = "T1", Description = "x", RequirementIds = new[] { "R9" } });

        var issues = SpecValidator.Validate(doc);
        Assert.Contains(issues, i => i.Code == "dangling-requirement-ref" && i.Severity == SpecSeverity.Error);
    }

    [Fact]
    public void TaskWithoutRequirement_IsError()
    {
        var doc = SpecDocument.Empty.WithTask(new TaskItem { Id = "T1", Description = "x" });
        Assert.Contains(SpecValidator.Validate(doc), i => i.Code == "task-without-requirement");
    }

    [Fact]
    public void ApprovedRequirementWithoutTask_IsCoverageWarning()
    {
        var doc = SpecDocument.Empty.WithRequirement(Req("R1", RequirementStatus.Approved));

        Assert.True(SpecValidator.IsValid(doc)); // warning only, not blocking
        Assert.Contains(SpecValidator.Validate(doc),
            i => i.Code == "uncovered-requirement" && i.Severity == SpecSeverity.Warning);
    }

    [Fact]
    public void DuplicateIds_AreErrors()
    {
        var doc = SpecDocument.Empty with
        {
            Requirements = new[] { Req("R1"), Req("R1") },
        };
        Assert.Contains(SpecValidator.Validate(doc), i => i.Code == "duplicate-requirement-id");
    }
}
