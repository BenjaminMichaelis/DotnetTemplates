namespace DotnetTool.Tests;

public class ToolTests
{
    [Fact]
    public void Greet_WithName_ReturnsGreeting()
    {
        string result = Tool.Greet("World");

        Assert.Equal("Hello, World!", result);
    }
}
