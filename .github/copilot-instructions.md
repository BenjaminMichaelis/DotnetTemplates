# Copilot instructions

This repository builds and publishes `BenjaminMichaelis.Dotnet.Templates`, a NuGet template pack for `dotnet new`.

## Repository structure

- `Templates.csproj` is the template-pack project. It packs files under `templates/**`.
- `templates/Library/NuGet` contains the `bmichaelis.nuget` template.
- `templates/Library/DotnetTool` contains the `bmichaelis.tool` template.
- `templates/Quickstart/ConsoleApp` contains the `bmichaelis.quickstart.consoleapp` template.
- `templates/Quickstart/BenchmarkApp` contains the `bmichaelis.quickstart.benchmarkconsole` template.
- `templates/Aspire/MinimalApi` contains the `bmichaelis.aspire.minimalapi` template.
- Each template has a `.template.config/template.json` file that controls `dotnet new` metadata, symbols, and conditional file inclusion.

## Development workflow

- Build the template package with `dotnet pack --configuration Release -o .`.
- Install a local package with `dotnet new install <package-name>.nupkg --force`.
- Generate each template and validate the supported options in `.template.config/template.json`.
- Run generated project builds/tests with warnings treated as errors when possible.

## Template authoring guidelines

- Keep `template.json` symbols and source modifiers covered by CI matrix variants.
- When adding an option such as `no-sln` or `no-tests`, test both the default path and the option-enabled path.
- Keep generated template files self-contained; do not rely on files outside the generated project.
- Keep template `Directory.Packages.props` and `Directory.Build.props` consistent across similar templates unless a template has a clear reason to differ.
- Copy root `.editorconfig` changes into templates with `CopyEditorConfigToTemplates.ps1`.

## Versioning and SDKs

- The root package project and generated templates target latest stable .net version.
- Keep templates on latest stable C# version unless a template has a specific reason to require a newer language version.
