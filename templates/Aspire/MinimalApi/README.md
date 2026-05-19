# Aspire Minimal API Template

This template creates a .NET Aspire solution with an ASP.NET Core Web API backend, EF Core, ASP.NET Core Identity, and Azure deployment assets.

## Template

Create a new app in your current directory by running:

```cli
> dotnet new bmichaelis.aspire.minimalapi
```

With Application Insights monitoring enabled:

```cli
> dotnet new bmichaelis.aspire.minimalapi --applicationInsights true
```

With Aspire integration tests:

```cli
> dotnet new bmichaelis.aspire.minimalapi --integrationTests true
```

Both options together:

```cli
> dotnet new bmichaelis.aspire.minimalapi --applicationInsights true --integrationTests true
```

### Parameters

[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--name` | Name of the project | Current directory name |
| `--applicationInsights` | Add Azure Application Insights monitoring | `false` |
| `--integrationTests` | Add Aspire integration tests using TUnit | `false` |

## What is included

- **API project** – ASP.NET Core Web API with controllers, CORS, Swagger/OpenAPI
- **ASP.NET Core Identity** – Cookie + JWT authentication for API endpoints
- **EF Core** – SQL Server data layer with AppHost-native `AddEFMigrations` orchestration
- **Aspire AppHost** – Local orchestration with SQL Server container and DB viewer (dbGate)
- **ServiceDefaults** – Shared OpenTelemetry, health checks, service discovery
- **Core + Tests** – Business logic projects with TUnit tests
- **Aspire integration tests** *(optional, `--integrationTests true`)* – TUnit-based tests that validate AppHost startup and distributed app behavior
- **Terraform/Infra** – Azure Container Apps, Azure SQL, Application Insights, ACR
- **GitHub Actions CI/CD** – Build, test, migrate DB, push container image

## Running locally

```cli
> aspire run
```

When you run locally, AppHost executes pending EF Core migrations through the `MinimalApi-backend-migrations` resource and starts the backend only after migrations finish.

When you run `aspire publish`, AppHost emits migration artifacts under `efmigrations/` (SQL script + migration bundle).

### Running tests

Run all tests (unit tests only when generated without `--integrationTests`):

```cli
> dotnet test
```

When generated with `--integrationTests true`, `MinimalApi.Aspire.Tests` is part of the solution, so `dotnet test` runs both unit and integration tests. To run only unit tests without starting containers:

```cli
> dotnet test MinimalApi.Core.Tests/MinimalApi.Core.Tests.csproj
```

To run only integration tests:

```cli
> dotnet test MinimalApi.Aspire.Tests/MinimalApi.Aspire.Tests.csproj
```

**Requirements for Aspire integration tests**:
- Docker must be running (Aspire uses containers for infrastructure)
- Sufficient disk space for SQL Server and other service containers
- First run may take 1–2 minutes to download and start containers

The AppHost passes the backend auth signing key through a secret parameter (`Auth__SigningKey`).
Set it once for local development:

```cli
> aspire secret set Parameters:auth-signing-key "<at-least-32-char-secret>"
```

## Container / Docker

A `Dockerfile` is included in `MinimalApi/` for building the API image outside of Aspire (CI/CD pipelines, Docker Compose, manual builds).

**Build the image** (run from the solution root — `templates/Aspire/MinimalApi/`):

```sh
docker build -f MinimalApi/Dockerfile -t myapp:latest .
```

**Run the image** (connection strings and secrets must be provided at runtime — never bake them in):

```sh
docker run -p 8080:8080 \
  -e Auth__SigningKey="<at-least-32-char-secret>" \
  -e ConnectionStrings__MinimalApi-db="Server=...;Database=...;" \
  myapp:latest
```

### Security notes
- Runtime image is `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` — no shell, no package manager, non-root (`app`, UID 65532)
- HTTPS is **not** terminated in the container; configure TLS at your ingress/load balancer
- All secrets must be injected via environment variables or a secrets manager (Azure Key Vault, Kubernetes secrets, etc.)

### Aspire vs. standalone Docker
When running with `aspire run`, the AppHost uses `AddProject<Projects.MinimalApi>` and Aspire handles container publishing automatically (no Dockerfile required). The `Dockerfile` is for non-Aspire workflows only.

The included GitHub Actions workflow uses Docker Buildx with GitHub Actions cache (`cache-from`/`cache-to`) and BuildKit cache-mount persistence for faster repeat builds in CI.

## Azure deployment

Infrastructure is managed with Terraform under `Infra/`. See `Infra/README.md` for setup instructions.

CI/CD deploys automatically on push to `main` via `.github/workflows/build-and-deploy.yml`.

## Application Insights

When the template is generated with `--applicationInsights true`, the following is added on top of the observability already included in `ServiceDefaults`:

| Addition | What it does |
|---|---|
| `Aspire.Hosting.Azure.ApplicationInsights` | Aspire provisions an AI resource in Azure and injects `APPLICATIONINSIGHTS_CONNECTION_STRING` automatically into the API container |
| `Microsoft.ApplicationInsights.Profiler.AspNetCore` | CPU flame-graph profiler; uploads traces to App Insights Performance blade |
| `builder.Services.AddServiceProfiler()` | Registers the profiler on Windows/Linux (skipped on unsupported platforms such as macOS) |

### How this works with Aspire's built-in telemetry

The template always configures a full OpenTelemetry pipeline in `ServiceDefaults`:

| Environment | Telemetry target | How |
|---|---|---|
| Local (`aspire run`) | **Aspire Dashboard** | Aspire injects `OTEL_EXPORTER_OTLP_ENDPOINT` automatically |
| Azure (Aspire publish) | **Application Insights** | `APPLICATIONINSIGHTS_CONNECTION_STRING` injected by Aspire resource reference |

The `UseAzureMonitor()` call in `ServiceDefaults/Extensions.cs` is already present and activates when the connection string env var is set — whether provided by Aspire or manually. **Do not also call `AddApplicationInsightsTelemetry()`** (the classic AI SDK); it conflicts with the OTel distro and double-reports all telemetry.

### Without the option (default)

The `UseAzureMonitor()` stub in `ServiceDefaults` still activates if you manually set `APPLICATIONINSIGHTS_CONNECTION_STRING` at runtime. You just won't have Aspire auto-provisioning or the profiler.
