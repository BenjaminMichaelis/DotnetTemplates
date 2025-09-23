# Dotnet Templates

The repository contains a set of opinionated [dotnet new templates](https://learn.microsoft.com/dotnet/core/tools/custom-templates). I am happy to receive critique/feedback on the existing templates, so feel free to open issues.

## Requirements

These templates require [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later.

## Installing

Use [dotnet new install](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-install) to install the templates.

```cli
> dotnet new install BenjaminMichaelis.Dotnet.Templates
```

To then use a template, navigate to a directory where you want to use the template and use the command

```cli
> dotnet new bmichaelis.{templatename}
```

with templatename being a name of one of the included templates listed below. Ex: `dotnet new bmichaelis.nuget`

## Included Templates (by template name, prepending `bmichaelis.` before the name as shown above)

- [nuget](./templates/Library/NuGet/README.md)
- [quickstart.benchmarkconsole](./templates/Quickstart/BenchmarkApp/README.md)
- [quickstart.consoleapp](./templates/Quickstart/ConsoleApp/README.md)

## Updating

If you have previously installed the templates and want to install the latest version, you can use [dotnet new update](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-update) to update your installed templates.

```cli
> dotnet new update
```

## Uninstalling

```cli
> dotnet new uninstall BenjaminMichaelis.Dotnet.Templates
```

## Local testing

Build the template package:

```cli
> dotnet pack --configuration Release -o .
```

Install the locally built template package

```cli
> dotnet new install . --force
```

You can now test the template by running:

```cli
> dotnet new bmichaelis.{templatename}
> dotnet build
> dotent test --no-build
> dotnet publish --no-build
```

When done, you can remove the local install of the template package by running:

```cli
> dotnet new uninstall .
```

## Adding a new template

- [ ] Add project under the template directory
- [ ] Update .template.config in directory
- [ ] Copy root .editorconfig to template directory (or run `./CopyEditorConfigToTemplates.ps1`)
- [ ] Update dependabot.yml
- [ ] Update build.yml workflow
- [ ] Add to README

### EditorConfig Management

Each template includes an `.editorconfig` file that is based on the root repository's `.editorconfig`. This ensures consistent coding standards across all generated projects.

**For new templates:**
- Copy the root `.editorconfig` to your template directory
- Or run `./CopyEditorConfigToTemplates.ps1` to copy to all templates

**For template-specific customizations:**
- Templates can have their own `.editorconfig` customizations (e.g., the NuGet template has more relaxed var usage rules)
- When updating the root `.editorconfig`, use the `CopyEditorConfigToTemplates.ps1` script which will detect and preserve existing customizations

**When updating root .editorconfig:**

**Automated Process (Recommended):**
- Simply update the root `.editorconfig` file and push to main
- The `update-template-editorconfig` GitHub Actions workflow will automatically:
  - Detect the changes and run the copy script
  - Create or update a PR with the template updates
  - Preserve existing customizations and create backups for manual review
  - Provide clear documentation of what was changed

**Manual Process:**
1. Update the root `.editorconfig` file
2. Run `./CopyEditorConfigToTemplates.ps1` 
3. Review any templates with customizations and manually merge changes if needed
4. Test all templates to ensure they work correctly
