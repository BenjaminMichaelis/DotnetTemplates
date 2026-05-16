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
4. Ensure template CI includes strict `dotnet format` checks (whitespace, style, analyzers)
5. Update CI matrix entries (for example, `build.yml`)
6. Update this README with the new template

## EditorConfig management

Templates include `.editorconfig` files based on the root `.editorconfig` for consistent defaults.

CI enforces formatting via strict checks:
- `dotnet format whitespace <target> --verify-no-changes --no-restore`
- `dotnet format style <target> --verify-no-changes --no-restore --severity info`
- `dotnet format analyzers <target> --verify-no-changes --no-restore --severity info`

**For template-specific customizations:**
- Keep template-specific rules in `templates/<Template>/.template.config/editorconfig.override`
- Override files are authoring metadata only; generated projects still contain a single final `.editorconfig`
- The NuGet template uses this pattern for its relaxed style severity and namespace preferences

**When updating root .editorconfig:**

**Automated Process (Recommended):**
- Simply update the root `.editorconfig` file and push to main
- The `update-template-editorconfig` GitHub Actions workflow will automatically:
  - Detect root or override changes and run the copy script
  - Create or update a PR with the template updates
  - Compose each template `.editorconfig` from root baseline + optional override
  - Provide clear documentation of what was changed

**Manual Process:**
1. Update the root `.editorconfig` file
2. If a template needs custom rules, edit `.template.config/editorconfig.override` for that template
3. Run `./CopyEditorConfigToTemplates.ps1` (rerun this script after any override edits)
4. Test all templates to ensure they work correctly
