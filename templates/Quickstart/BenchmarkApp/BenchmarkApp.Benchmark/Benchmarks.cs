using BenchmarkDotNet.Attributes;

namespace BenchmarkApp;

#pragma warning disable CA1822 // BenchmarkDotNet requires instance methods.
public class Benchmarks
{
    [Benchmark]
    public void Scenario1()
    {
        // Implement your benchmark here
    }

    [Benchmark]
    public void Scenario2()
    {
        // Implement your benchmark here
    }
}
#pragma warning restore CA1822
