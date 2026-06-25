using Agora.Cli;
using Agora.Rag.Concretes;
using Agora.Rag.Models;
using Xunit;

namespace Agora.Tests.Cli;

public class CliPurgeKbLogTests
{
    [Fact]
    public async Task PurgeKbLog_NoFilter_RemovesAllAndReports()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var configPath = Path.Combine(dir, "agora.yaml"); // only its directory is used by the verb
        var log = new FileKbMutationLog(FileKbMutationLog.DefaultPath(dir));
        try
        {
            await log.AppendAsync(new KbMutation { Kind = KbMutationKind.Add, AgentId = "a", NewText = "x" });
            await log.AppendAsync(new KbMutation { Kind = KbMutationKind.Replace, AgentId = "b", NewText = "y" });

            var outw = new StringWriter();
            var code = CliRunner.Run(new[] { "purge-kb-log", "--config", configPath }, null, outw, new StringWriter());

            Assert.Equal(0, code);
            Assert.Contains("purged 2", outw.ToString());
            Assert.Empty(await new FileKbMutationLog(FileKbMutationLog.DefaultPath(dir)).ReadAllAsync());
        }
        finally { Directory.Delete(dir, true); }
    }
}
