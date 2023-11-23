# Dotnet Templates

The repository contains a set of opinionated [dotnet new templates](https://learn.microsoft.com/dotnet/core/tools/custom-templates). I am happy to receive critique/feedback on the existing templates, so feel free to open issues.

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

## Included Templates (by template name)

- [nuget](./templates/Library/NuGet/README.md)
- [quickstart.benchmarkconsole](./templates/Quickstart/BenchmarkApp/README.md)

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
- [ ] Update dependabot.yml
- [ ] Add to README
