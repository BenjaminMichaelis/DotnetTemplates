---
name: new-template
description: Step-by-step checklist for adding a new dotnet new template to the BenjaminMichaelis/DotnetTemplates repository. Use this skill when asked to create, add, or scaffold a new template in this repo.
---

# Adding a new `dotnet new` template

Follow every item in this checklist. Do not open a PR until all items are done.

## 1. Versions — always use the latest

- **.NET TFM**: use the latest stable version (e.g. `net10.0`) in every `.csproj`.
- **C# LangVersion**: use the latest stable version (e.g. `14`) in `Directory.Build.props`.
- **SDK in `global.json`**: match the latest SDK version used by other templates in the repo; keep `rollForward: latestMinor`.
- **NuGet packages in `Directory.Packages.props`**: check nuget.org for the latest stable version of every package before writing it. Do not copy an old version from another template without verifying it is current.

## 2. Template files

- `templates/<Category>/<Name>/` — all template source files live here.
- `.template.config/template.json` — short name, parameters, and source modifiers.
  - Every parameter combination must be covered by a CI matrix variant.
  - When adding `--no-tests`, do **not** exclude the solution file or CI workflow — use in-file `#if (!no-tests)` guards instead.
- `global.json` — SDK version + `"test": { "runner": "Microsoft.Testing.Platform" }`.
- `Directory.Build.props` — `LangVersion`, `net<X>.0`, NuGet metadata placeholders.
- `Directory.Packages.props` — central package management with latest package versions.
- `.editorconfig` — **do not write this file by hand.** Run `.\CopyEditorConfigToTemplates.ps1` from the repo root to regenerate it. The script copies the root `.editorconfig` into every template directory, then appends any template-specific rules from `.template.config/editorconfig.override`. If the new template needs style relaxations, add them to `.template.config/editorconfig.override` (additive only; never include `root = ...`). CI enforces `end_of_line = lf` and `insert_final_newline = true` via `dotnet format --verify-no-changes` on ubuntu-latest; CRLF line endings will cause failures.
- `.gitignore`, `NuGet.config`, `README.md`.
- `.config/dotnet-tools.json` — include `trx-to-vsplaylist` 1.3.0 so generated repos can produce failure playlists.

## 3. CI workflows (inside the template's `.github/workflows/`)

- **`build-and-test.yml`**:
  - `permissions: contents: read`
  - `actions/checkout@v6`
  - Restore .NET tools (`dotnet tool restore`)
  - Enforce formatting (`.\.github\scripts\Invoke-DotNetFormatStrict.ps1`)
  - Build with `-p:ContinuousIntegrationBuild=True`
  - Test with MTP flags: `--report-trx --report-trx-filename tests.trx` (or `--report-xunit-trx` for xunit)
  - Generate merged failure playlist from TRX files → `test-results/failed-tests.playlist`
  - Upload playlist artifact on failure with `actions/upload-artifact@v7`
  - Wrap Test/playlist/upload steps in `#if (!no-tests)` if the template has a `--no-tests` option
- **`deploy.yml`**: `permissions: contents: read`, `actions/checkout@v6`
- **`copilot-setup-steps.yml`**: `actions/checkout@v6`

## 4. Root-level wiring (required before PR)

- **`.github/workflows/build.yml`**:
  - Add a `test-<templatename>` matrix job covering all parameter variants (default + each opt-in/opt-out combination).
  - Add the new job to the `all-tests` gate job's `needs` list.
- **`.github/dependabot.yml`** — add the template to **all three** ecosystems:
  - `github-actions` → add the template's `.github/workflows` directory to the `directories` list.
  - `nuget` → add the template root directory to the `directories` list.
  - `dotnet-sdk` → add the template root directory to the `directories` list.
- **Root `README.md`** — add a row for the new template in the templates table.
- **`.github/copilot-instructions.md`** — add the template short name and path to the repository structure list.

## 5. Validation

```powershell
# Pack
dotnet pack --configuration Release -o .

# Install
dotnet new install .\BenjaminMichaelis.Dotnet.Templates.*.nupkg --force

# Generate and build every matrix variant
$outDir = Join-Path $env:TEMP "template-test"
dotnet new <shortname> -n MyProject -o $outDir
cd $outDir
dotnet build --configuration Release
dotnet test --configuration Release
```

- Generated project must build and test with zero warnings (warnings are errors).
- `dotnet format --verify-no-changes` must pass on Linux (lf line endings, final newlines).
