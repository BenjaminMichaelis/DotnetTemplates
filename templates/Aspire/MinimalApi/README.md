# Aspire Minimal API Template

This template creates a .NET Aspire solution with an ASP.NET Core Web API backend, EF Core, ASP.NET Core Identity, and Azure deployment assets.

## Template

Create a new app in your current directory by running:

```cli
> dotnet new bmichaelis.aspire.minimalapi
```

### Parameters

[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--name` | Name of the project | Current directory name |

## What is included

- **API project** – ASP.NET Core Web API with controllers, CORS, Swagger/OpenAPI
- **ASP.NET Core Identity** – Cookie + JWT authentication for API endpoints
- **EF Core** – SQL Server data layer with migrations via AppHost
- **Aspire AppHost** – Local orchestration with SQL Server container and DB viewer (dbGate)
- **ServiceDefaults** – Shared OpenTelemetry, health checks, service discovery
- **Core + Tests** – Business logic projects with TUnit tests
- **Terraform/Infra** – Azure Container Apps, Azure SQL, Application Insights, ACR
- **GitHub Actions CI/CD** – Build, test, migrate DB, push container image

## Running locally

```cli
> aspire run
```

## Azure deployment

Infrastructure is managed with Terraform under `Infra/`. See `Infra/README.md` for setup instructions.

CI/CD deploys automatically on push to `main` via `.github/workflows/build-and-deploy.yml`.
