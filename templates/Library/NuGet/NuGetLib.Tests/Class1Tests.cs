namespace NuGetLib.Tests;

public class Class1Tests
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

        Class1 class1 = mocker.CreateInstance<Class1>();

        //Act
        int result = class1.Method(41);

        //Assert
        await AssertEqual(result, 42);
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
