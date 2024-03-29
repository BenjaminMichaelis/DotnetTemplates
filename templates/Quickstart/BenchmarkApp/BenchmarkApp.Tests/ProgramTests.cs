namespace BenchmarkApp.Console.Tests;

public class ProgramTests
{
    [Fact]
    public void Method_WithPositiveValue_AddsOne()
    {
        //Arrange
        AutoMocker mocker = new();

        Program class1 = mocker.CreateInstance<Program>();

        //Act
        int result = class1.Method(41);

        //Assert
        Assert.Equal(42, result);
    }
}