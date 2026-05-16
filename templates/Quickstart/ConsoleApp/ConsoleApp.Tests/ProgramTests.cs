namespace ConsoleApp.Console.Tests;

public class ProgramTests
{
#if USE_XUNIT
    [Fact]
#else
    [Test]
#endif
    public async Task Method_WithPositiveValue_AddsOne()
    {
        //Arrange
        AutoMocker mocker = new();

        Program class1 = mocker.CreateInstance<Program>();

        //Act
        int result = class1.Method(41);

        //Assert
        await AssertEqual(result, 42);
    }

#if USE_XUNIT
    [Fact]
#else
    [Test]
#endif
    public async Task Method_WithNegativeValue_AddsOne()
    {
        //Arrange
        Program class1 = new();
        //Act
        int result = class1.Method(-2);
        //Assert
        await AssertEqual(result, -1);
    }

    private static async Task AssertEqual(int actual, int expected)
    {
#if USE_XUNIT
        Assert.Equal(expected, actual);
        await Task.CompletedTask;
#else
        await Assert.That(actual).IsEqualTo(expected);
#endif
    }

}
