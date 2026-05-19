using NuGetLib;

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
