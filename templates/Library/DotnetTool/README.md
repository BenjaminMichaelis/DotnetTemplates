# DotnetTool

A base template for .NET Global Tool packages, with unit tests and GitHub Actions. Supports RID-specific packaging so the tool installs a platform-optimized binary on `win-x64`, `linux-x64`, and `osx-arm64`, with a portable `any` CoreCLR fallback for all other platforms (e.g. `win-arm64`, `linux-arm64`).

> **Requires .NET SDK 10.0 or later.** RID-specific tool packaging is a .NET SDK 10 feature.
> See [RID-specific tools](https://learn.microsoft.com/en-us/dotnet/core/tools/rid-specific-tools) for background.

## Startup Steps

- [ ] Search for all CHANGEME words — most are in `Directory.Build.props` and `deploy.yml`
- [ ] Set `<ToolCommandName>` in `DotnetTool.csproj` to your desired CLI command name
- [ ] Make sure the project compiles and tests pass
- [ ] Add `NUGET_API_KEY` with your NuGet.org API key to the GitHub repository secrets

## Features

- `PackAsTool` with `RuntimeIdentifiers` for `win-x64`, `linux-x64`, `osx-arm64`, and `any` (portable fallback)
- A single `dotnet pack` produces all RID-specific packages plus a top-level pointer package
- GitHub Actions deploy workflow that publishes RID-specific packages before the pointer package (required ordering)
- Central Package Management (`Directory.Packages.props`)
- SourceLink for debuggable symbols
- TUnit test project (xunit v3 available via `--tests xunit`), with Moq.AutoMock

## Template

Create a new tool in your current directory by running:

```cli
dotnet new bmichaelis.tool
```

## Building and packing locally

```cli
dotnet build
dotnet test --no-build
dotnet pack --configuration Release -o ./artifacts/package/release
```

This creates:
- `DotnetTool.win-x64.<version>.nupkg`
- `DotnetTool.linux-x64.<version>.nupkg`
- `DotnetTool.osx-arm64.<version>.nupkg`
- `DotnetTool.any.<version>.nupkg` (portable CoreCLR fallback)
- `DotnetTool.<version>.nupkg` (top-level pointer package)

> **Note:** `dotnet pack` may emit `NU5017` on the pointer package. This is a known false positive in .NET SDK 10 — NuGet validation does not yet recognise `DotnetToolSettings.xml` as package content. All packages are created correctly and the pointer package installs as expected.

## Installing the tool locally

```cli
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="./artifacts/package/release" />
  </packageSources>
</configuration>
"@ | Set-Content ./NuGet.local.config

dotnet tool install --global DotnetTool --configfile ./NuGet.local.config
```

## Generating Releases

1. Create a release on GitHub
2. Add your [NuGet.org](https://www.nuget.org/account/apikeys) API key to the GitHub repository secrets as `NUGET_API_KEY` with push scope permissions
3. Tag the release following `v*.*.*` format, e.g. `v1.0.0`

The deploy workflow publishes RID-specific packages first, then the pointer package last. This ordering is required — if the pointer package is published before its sub-packages, installs will fail.
