# Dotnet Templates

This repository contains opinionated [`dotnet new` templates](https://learn.microsoft.com/dotnet/core/tools/custom-templates), published as the `BenjaminMichaelis.Dotnet.Templates` NuGet template pack.

Feedback and contributions are welcome via issues and pull requests.

## Requirements

Use [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or newer.

## Install

Install the template pack:

```cli
> dotnet new install BenjaminMichaelis.Dotnet.Templates
```

Create a project from a template:

```cli
> dotnet new bmichaelis.{template-name}
```

Example:

```cli
> dotnet new bmichaelis.nuget
```

## Included templates

Use the short name after `bmichaelis.`:

| Short name | Template docs |
| --- | --- |
| `nuget` | [templates/Library/NuGet](./templates/Library/NuGet/README.md) |
| `quickstart.consoleapp` | [templates/Quickstart/ConsoleApp](./templates/Quickstart/ConsoleApp/README.md) |
| `quickstart.benchmarkconsole` | [templates/Quickstart/BenchmarkApp](./templates/Quickstart/BenchmarkApp/README.md) |
| `aspire.minimalapi` | [templates/Aspire/MinimalApi](./templates/Aspire/MinimalApi/README.md) |

## Update installed templates

```cli
> dotnet new update
```

## Uninstall

```cli
> dotnet new uninstall BenjaminMichaelis.Dotnet.Templates
```

## Local testing

Build the local package:

```cli
> dotnet pack --configuration Release -o .
```

Install the local package:

```cli
> dotnet new install .\BenjaminMichaelis.Dotnet.Templates.*.nupkg --force
```

Generate and validate:

```cli
> dotnet new bmichaelis.{template-name}
> dotnet build
> dotnet test --no-build
> dotnet publish --no-build
```

Remove the local install:

```cli
> dotnet new uninstall .\BenjaminMichaelis.Dotnet.Templates.*.nupkg
```

## Repository layout

- `Templates.csproj`: template pack project that packs `templates/**`
- `templates/Library/NuGet`: `bmichaelis.nuget`
- `templates/Quickstart/ConsoleApp`: `bmichaelis.quickstart.consoleapp`
- `templates/Quickstart/BenchmarkApp`: `bmichaelis.quickstart.benchmarkconsole`
- `templates/Aspire/MinimalApi`: `bmichaelis.aspire.minimalapi`

## Adding a new template

1. Add the template files under `templates/`
2. Add or update `.template.config/template.json`
3. Copy the root `.editorconfig` to the template (or run `.\CopyEditorConfigToTemplates.ps1`)
4. Update CI matrix entries (for example, `build.yml`)
5. Update this README with the new template

## EditorConfig management

Templates include `.editorconfig` files based on the root `.editorconfig` for consistent defaults.

Recommended workflow:

1. Update the root `.editorconfig`
2. Run `.\CopyEditorConfigToTemplates.ps1`
3. Review template-specific customizations
4. Validate template generation and builds

An automated `update-template-editorconfig` workflow also syncs template editorconfig files and opens/updates a pull request when needed.
