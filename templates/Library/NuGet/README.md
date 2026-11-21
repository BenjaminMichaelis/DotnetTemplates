# NuGet Library

A base template for NuGet Library Packages, with unit tests, and github actions

## Startup Steps

- [ ] Search for all CHANGEME words, most are in the Directory.Build.props file and the deploy workflow
- [ ] Make sure the project compiles properly
- [ ] Configure NuGet trusted publishing and the `NUGET_USER` environment secret as described below

## Features

## Template

Create a new app in your current directory by running.

```cli
 dotnet new bmichaelis.nuget
```

## .NET SDK version

This template includes a `global.json` file. To update the SDK version used by generated projects and CI validation, update `global.json`.

### Parameters

- `--no-sln`: Don't include the solution file.
- `--sln`: Include a legacy `.sln` file instead of the default `.slnx` file.
- `--tests`: Choose the test framework (`tunit` default, `xunit` for xUnit v3 + MTP v2, `None` to exclude test project).

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

   Use the workflow filename, not its `name:` or full path. The policy's individual or organization account must own the package being published. For private repositories, the policy is temporarily active for seven days and becomes permanent after the first successful publish.
3. Before your first publish, run `dotnet pack -c Release -o ./artifacts` to verify the `.nupkg` is created correctly.

When migrating an existing repository, retain its old API key secret until a trusted-publishing release has succeeded.

### Generating Releases

After confirming that the policy and environment secret are configured, create a GitHub release with a [SemVer](https://semver.org/) tag such as `v1.0.0`. The existing deploy workflow packs and publishes that version. Confirm the package appears on nuget.org.
