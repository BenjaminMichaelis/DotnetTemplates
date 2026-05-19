namespace DotnetTool.Tests;

using DotnetTool;

public class ToolTests
{
#if USE_XUNIT
    [Fact]
#else
    [Test]
#endif
    public async Task Greet_WithName_ReturnsGreeting()
    {
        string result = Tool.Greet("World");

#if USE_XUNIT
        Assert.Equal("Hello, World!", result);
        await Task.CompletedTask;
#else
        await Assert.That(result).IsEqualTo("Hello, World!");
#endif
    }
}
