namespace DotnetTool.Tests;

using DotnetTool;

public class ToolTests
{
#if USE_XUNIT
    [Fact]
#else
    [Test]
#endif
    public async Task GreetWithNameReturnsGreeting()
    {
        string result = Tool.Greet("World");

#if USE_XUNIT
        Xunit.Assert.Equal("Hello, World!", result);
        await Task.CompletedTask;
#else
        await TUnit.Assertions.Assert.That(result).IsEqualTo("Hello, World!");
#endif
    }
}
