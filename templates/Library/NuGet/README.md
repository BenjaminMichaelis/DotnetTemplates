# NuGet Library

A base template for NuGet Library Packages

## Startup Steps

- [ ] Search for all CHANGEME words, most are in the Directory.Build.props file and the deploy workflow
- [ ] Make sure the project compiles properly
- [ ] Add NUGET_API_KEY with your NUGET api key to the GitHub repository to be able to deploy packages to Nuget.org

## Features

### Generating Releases

1. Create a release on GitHub
2. Tag the release with a tag following a v*.*.*format, likely with*.*.* following a [Semver](https://semver.org/) format.
