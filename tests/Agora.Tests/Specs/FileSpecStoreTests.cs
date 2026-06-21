using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;

namespace Agora.Tests.Specs;

public class FileSpecStoreTests
{
    [Fact]
    public async Task RoundTrips_Requirements_Tasks_And_Enums()
    {
        var path = Path.Combine(Path.GetTempPath(), $"agora-spec-{Guid.NewGuid():N}.json");
        try
        {
            var store = new FileSpecStore(path);
            var doc = SpecDocument.Empty
                .WithRequirement(new Requirement
                {
                    Id = "R1",
                    Title = "Login",
                    Priority = RequirementPriority.Should,
                    Status = RequirementStatus.Approved,
                    AcceptanceCriteria = new[]
                    {
                        new AcceptanceCriterion("R1.A1", "session is created",
                            new SpecCheck(CheckKind.Test, "test:auth")),
                    },
                })
                .WithTask(new TaskItem
                {
                    Id = "T1",
                    Description = "implement login",
                    Kind = "be",
                    RequirementIds = new[] { "R1" },
                    Status = TaskState.InProgress,
                });

            await store.SaveAsync(doc);

            var reloaded = await new FileSpecStore(path).LoadAsync();
            var r = reloaded.FindRequirement("R1")!;
            Assert.Equal(RequirementPriority.Should, r.Priority);
            Assert.Equal(RequirementStatus.Approved, r.Status);
            Assert.Equal(CheckKind.Test, r.AcceptanceCriteria[0].Check.Kind);
            Assert.Equal("test:auth", r.AcceptanceCriteria[0].Check.Expression);
            Assert.Equal(TaskState.InProgress, reloaded.FindTask("T1")!.Status);
            Assert.Equal(new[] { "R1" }, reloaded.FindTask("T1")!.RequirementIds);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task Load_MissingFile_ReturnsEmpty()
    {
        var store = new FileSpecStore(Path.Combine(Path.GetTempPath(), $"agora-missing-{Guid.NewGuid():N}.json"));
        var doc = await store.LoadAsync();
        Assert.Empty(doc.Requirements);
        Assert.Empty(doc.Tasks);
    }
}
