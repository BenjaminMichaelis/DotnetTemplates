using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkApp;

public class Program
{
    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance;
        var summary = BenchmarkRunner.Run<Md5VsSha256>(config, args);
        var summary2 = BenchmarkRunner.Run<Benchmarks>(config, args);

        // Use this to select benchmarks from the console:
        // var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
