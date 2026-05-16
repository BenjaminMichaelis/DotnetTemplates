# Benchmark app template

This template creates a [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) solution, along with a console project with unit tests.

The point of this is so that you can easily create a benchmark project to test out different algorithms, or to test out different ways of doing things.

## Startup
- To run the benchmarks, run `dotnet run -c Release` in the `BenchmarkApp` folder.

## Template

Create a new app in your current directory by running.

```cli
 dotnet new bmichaelis.quickstart.benchmarkconsole
```

### Parameters

- `--sln`: Include a legacy `.sln` file instead of the default `.slnx` file.
- `--no-sln`: Don't include a solution file.
- `--no-tests`: Don't include the test project.
- `--tests`: Choose the test framework (`tunit` default, `xunit` for xUnit v3 + MTP v2).

[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

## Key Features

- Should just work out of the box. Not meant to be the perfect project setup to start with, but rather a quick way to get started with benchmarking and testing out different algorithms or such.
- Includes unit tests using TUnit by default, with xUnit v3 + MTP v2 available via `--tests xunit`.
