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

## Adding a new template

When creating a new `dotnet new` template, go through this checklist before opening a PR:

### Template files
- Use the latest stable .NET TFM (currently `net10.0`) in all `.csproj` files.
- Set `<LangVersion>` to the latest stable C# version (currently `14`) in `Directory.Build.props`.
- Use the latest versions of all NuGet packages in `Directory.Packages.props` — check nuget.org before adding a new package.
- Add `global.json` with the current SDK version, `rollForward: latestMinor`, and `"test": { "runner": "Microsoft.Testing.Platform" }`.
- Add `.editorconfig` with `end_of_line = lf` and `insert_final_newline = true` (CI runs `dotnet format` on ubuntu-latest).
- Add `.config/dotnet-tools.json` with `trx-to-vsplaylist` so the generated repo can produce failure playlists.

### CI workflows (inside the template's `.github/workflows/`)
- `build-and-test.yml`: `permissions: contents: read`, `checkout@v6`, `dotnet tool restore`, format check, build, test with MTP flags (`--report-trx` or `--report-xunit-trx`), TRX playlist generation, `upload-artifact@v7`.
- `deploy.yml`: `permissions: contents: read`, `checkout@v6`.
- `copilot-setup-steps.yml`: `checkout@v6`.

### Root-level wiring (every item required)
- Add the template to `.github/workflows/build.yml`: a matrix job covering all parameter variants, and add that job to the `all-tests` gate `needs` list.
- Add the template to `.github/dependabot.yml` under **all three** ecosystems:
  - `github-actions` — the template's `.github/workflows` directory
  - `nuget` — the template root directory
  - `dotnet-sdk` — the template root directory
- Add the template's short name and path to the root `README.md` table.
- Add the template to the repository structure list in `.github/copilot-instructions.md`.
