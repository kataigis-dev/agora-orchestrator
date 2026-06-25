using Agora.Rag.Concretes;
using Agora.Rag.Models;
using Xunit;

namespace Agora.Tests.Rag;

public class FileKbMutationLogTests
{
    private static string TempPath() => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".jsonl");

    [Fact]
    public async Task Append_IsAppendOnly_AndSurvivesRestart()
    {
        var path = TempPath();
        try
        {
            var log = new FileKbMutationLog(path);
            await log.AppendAsync(new KbMutation { Kind = KbMutationKind.Add, AgentId = "a", NewText = "one" });
            await log.AppendAsync(new KbMutation
            {
                Kind = KbMutationKind.Replace, AgentId = "b", NewText = "two", OldText = new[] { "one" },
                Decision = "kept new", JudgeExplanation = "contradiction",
            });

            // A fresh instance reads both records back, in order, with all fields intact.
            var reopened = await new FileKbMutationLog(path).ReadAllAsync();
            Assert.Equal(2, reopened.Count);
            Assert.Equal("one", reopened[0].NewText);
            Assert.Equal(KbMutationKind.Replace, reopened[1].Kind);
            Assert.Equal(new[] { "one" }, reopened[1].OldText);
            Assert.Equal("kept new", reopened[1].Decision);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Purge_RemovesOnlyMatchingRecords()
    {
        var path = TempPath();
        try
        {
            var log = new FileKbMutationLog(path);
            await log.AppendAsync(new KbMutation
            {
                Kind = KbMutationKind.Add, AgentId = "a", NewText = "old",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-10),
            });
            await log.AppendAsync(new KbMutation
            {
                Kind = KbMutationKind.Add, AgentId = "a", NewText = "recent", Timestamp = DateTimeOffset.UtcNow,
            });

            var cutoff = DateTimeOffset.UtcNow.AddDays(-1);
            Assert.Equal(1, await log.PurgeAsync(m => m.Timestamp < cutoff));

            var remaining = await new FileKbMutationLog(path).ReadAllAsync();
            Assert.Equal("recent", Assert.Single(remaining).NewText);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task PurgeAll_EmptiesTheLog()
    {
        var path = TempPath();
        try
        {
            var log = new FileKbMutationLog(path);
            await log.AppendAsync(new KbMutation { Kind = KbMutationKind.Add, AgentId = "a", NewText = "x" });
            Assert.Equal(1, await log.PurgeAsync(_ => true));
            Assert.Empty(await log.ReadAllAsync());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ReadAll_MissingFile_IsEmpty()
        => Assert.Empty(await new FileKbMutationLog(TempPath()).ReadAllAsync());
}
