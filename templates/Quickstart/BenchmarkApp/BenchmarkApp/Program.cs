using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkApp;

public class Md5VsSha256
{
    private const int N = 10000;
    private readonly byte[] _data;

    private readonly SHA256 _sha256 = SHA256.Create();
    private readonly MD5 _md5 = MD5.Create();

    public Md5VsSha256()
    {
        _data = new byte[N];
        new Random(42).NextBytes(_data);
    }

    [Benchmark]
    public byte[] Sha256() => _sha256.ComputeHash(_data);

    [Benchmark]
    public byte[] Md5() => _md5.ComputeHash(_data);
}

public class NewListVsCollectionInitializer
{
    [Benchmark]
    public List<string> NewList() => new() { "a", "b", "c" };

    [Benchmark]
    public List<string> CollectionInitializedList() => ["a", "b", "c"];
}

public class Program
{
    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance;
        var summary = BenchmarkRunner.Run<Md5VsSha256>(config, args);
        var summary2 = BenchmarkRunner.Run<NewListVsCollectionInitializer>(config, args);

        // Use this to select benchmarks from the console:
        // var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
