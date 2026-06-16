using Xunit;

namespace Agora.Tests;

public class AgoraInfoTests
{
    [Fact]
    public void Version_IsExposed()
    {
        Assert.False(string.IsNullOrEmpty(AgoraInfo.Version));
    }
}
