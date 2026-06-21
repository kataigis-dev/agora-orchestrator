using Agora.Specs;

namespace Agora.Tests.Specs;

public class SpecDocumentTests
{
    [Fact]
    public void WithRequirement_UpsertsById_PreservingOrder()
    {
        var doc = SpecDocument.Empty
            .WithRequirement(new Requirement { Id = "R1", Title = "first" })
            .WithRequirement(new Requirement { Id = "R2", Title = "second" })
            .WithRequirement(new Requirement { Id = "R1", Title = "updated" });

        Assert.Equal(2, doc.Requirements.Count);
        Assert.Equal("updated", doc.FindRequirement("R1")!.Title);
        Assert.Equal("R1", doc.Requirements[0].Id); // order preserved on update
    }

    [Fact]
    public void NextIds_AreMaxPlusOne_StableAcrossDeletion()
    {
        var doc = SpecDocument.Empty
            .WithRequirement(new Requirement { Id = "R1", Title = "a" })
            .WithRequirement(new Requirement { Id = "R2", Title = "b" });
        Assert.Equal("R3", doc.NextRequirementId());

        // Drop R2; the next id must not collide with the once-used R2.
        var pruned = doc with { Requirements = new[] { doc.FindRequirement("R1")! } };
        Assert.Equal("R2", pruned.NextRequirementId());
        Assert.Equal("T1", pruned.NextTaskId());
    }

    [Fact]
    public void WithTask_UpsertsById()
    {
        var doc = SpecDocument.Empty
            .WithTask(new TaskItem { Id = "T1", Description = "x", RequirementIds = new[] { "R1" } })
            .WithTask(new TaskItem { Id = "T1", Description = "y", RequirementIds = new[] { "R1", "R2" } });

        Assert.Single(doc.Tasks);
        Assert.Equal("y", doc.FindTask("T1")!.Description);
        Assert.Equal(2, doc.FindTask("T1")!.RequirementIds.Count);
    }
}
