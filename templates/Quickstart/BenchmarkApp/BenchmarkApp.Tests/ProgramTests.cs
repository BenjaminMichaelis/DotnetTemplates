using BenchmarkApp.Console;

namespace BenchmarkApp.Console.Tests;

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
        await AssertEqual(42, result);
    }

    private static async Task AssertEqual(int expected, int actual)
    {
#if USE_XUNIT
        Xunit.Assert.Equal(expected, actual);
        await Task.CompletedTask;
#else
        await TUnit.Assertions.Assert.That(actual).IsEqualTo(expected);
#endif
    }

}
