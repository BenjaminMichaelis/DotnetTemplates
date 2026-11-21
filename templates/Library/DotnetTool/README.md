# DotnetTool

A base template for .NET Global Tool packages, with unit tests and GitHub Actions. Supports RID-specific packaging so the tool installs a platform-optimized binary on `win-x64`, `linux-x64`, and `osx-arm64`, with a portable `any` CoreCLR fallback for all other platforms (e.g. `win-arm64`, `linux-arm64`).

> **Requires .NET SDK 11.0 or later.** RID-specific tool packaging was introduced in .NET SDK 10.
> See [RID-specific tools](https://learn.microsoft.com/en-us/dotnet/core/tools/rid-specific-tools) for background.

## Startup Steps

- [ ] Search for all CHANGEME words — most are in `Directory.Build.props` and `deploy.yml`
- [ ] Set `<ToolCommandName>` in `DotnetTool.csproj` to your desired CLI command name
- [ ] Make sure the project compiles and tests pass
- [ ] Configure NuGet trusted publishing and the `NUGET_USER` environment secret as described below

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

## .NET SDK version

This template includes a `global.json` file. To update the SDK version used by generated projects and CI validation, update `global.json`.

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

## NuGet trusted publishing

The deploy workflow uses [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) with `NuGet/login@v1` and job-scoped `id-token: write` permission. It exchanges a GitHub OIDC token for a short-lived API key instead of storing a long-lived API key.

1. In GitHub repository **Settings > Environments**, create the `Production` environment. Add an environment secret named `NUGET_USER` containing your nuget.org profile name, not your email address. Optionally add required reviewers.
2. At [nuget.org/account/trustedpublishing](https://www.nuget.org/account/trustedpublishing), add a policy with these values, replacing the repository placeholders with your generated project's GitHub repository:

   | Field | Value |
   | --- | --- |
   | Repository Owner | `CHANGEME_RepoOwner` |
   | Repository | `CHANGEME_RepoName` |
   | Workflow File | `deploy.yml` |
   | Environment | `Production` |

   Use the workflow filename, not its `name:` or full path. The policy's individual or organization account must own every package being published, including the RID-specific packages. For private repositories, the policy is temporarily active for seven days and becomes permanent after the first successful publish.
3. Before your first publish, run `dotnet pack -c Release -o ./artifacts` to verify the `.nupkg` files are created correctly. Use the local installation instructions above with that output directory to check the tool before publishing.

When migrating an existing repository, retain its old API key secret until a trusted-publishing release has succeeded.

## Generating Releases

After confirming that the policy and environment secret are configured, create a GitHub release with a [SemVer](https://semver.org/) tag such as `v1.0.0`. The existing deploy workflow packs and publishes that version. Confirm the packages appear on nuget.org.

The deploy workflow publishes RID-specific packages first, then the pointer package last. This ordering is required — if the pointer package is published before its sub-packages, installs will fail.
