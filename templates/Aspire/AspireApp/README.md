# AspireApp Template

This template creates a full-stack .NET Aspire solution with an ASP.NET Core Web API backend, an optional React frontend, EF Core, ASP.NET Core Identity, and Azure deployment assets.

## Template

Create a new app in your current directory by running:

```cli
> dotnet new bmichaelis.aspire.app
```

With the default React frontend:

```cli
> dotnet new bmichaelis.aspire.app --frontend react
```

Without a frontend (API-only solution):

```cli
> dotnet new bmichaelis.aspire.app --frontend none
```

With Application Insights monitoring enabled:

```cli
> dotnet new bmichaelis.aspire.app --applicationInsights true
```

With Aspire integration tests:

```cli
> dotnet new bmichaelis.aspire.app --integrationTests true
```

Both options together:

```cli
> dotnet new bmichaelis.aspire.app --applicationInsights true --integrationTests true
```

### Parameters

[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--name` | Name of the project | Current directory name |
| `--frontend` | Frontend to generate: `react` for the default React + TypeScript app, or `none` for an API-only solution | `react` |
| `--applicationInsights` | Add Azure Application Insights monitoring | `false` |
| `--integrationTests` | Add Aspire integration tests using TUnit | `false` |

## What is included

- **API project** – ASP.NET Core Web API with controllers, CORS, OpenAPI, and Scalar API docs (development)
- **ASP.NET Core Identity** – Cookie + JWT authentication for API endpoints
- **EF Core** – SQL Server data layer with AppHost-native `AddEFMigrations` orchestration
- **Aspire AppHost** – Local orchestration with SQL Server container and DB viewer (dbGate)
- **ServiceDefaults** – Shared OpenTelemetry, health checks, service discovery
- **Core + Tests** – Business logic projects with TUnit tests
- **Aspire integration tests** *(optional, `--integrationTests true`)* – TUnit-based tests that validate AppHost startup and distributed app behavior
- **Terraform/Infra** – Azure Container Apps, Azure SQL, Application Insights, ACR
- **GitHub Actions CI/CD** – Build, test, migrate DB, push container image

## Running locally

The AppHost uses NuGet-restored orchestration dependencies, so `dotnet run` and IDE launches do not require an Aspire CLI bundle. Only the ASPIRE010 warning is suppressed; other build diagnostics remain enabled. The Aspire CLI is required for the `aspire` commands below.

With the React frontend, install Node.js 24 and the pnpm version pinned in `AspireApp.Web/package.json`. From the solution root:

```cli
> npm install --global pnpm@11.1.3
```

Frontend builds restore dependencies using the frozen lockfile. The included build workflow provisions Node.js and the pinned pnpm automatically; API-only generated apps do not require either.

```cli
> aspire run
```

When you run locally, AppHost executes pending EF Core migrations through the `AspireApp-backend-migrations` resource and starts the backend only after migrations finish.

When you run `aspire publish`, AppHost emits migration artifacts under `efmigrations/` (SQL script + migration bundle).

In development, the API exposes:
- OpenAPI document: `/openapi/v1.json`
- Scalar UI: `/scalar`

### Running tests

Run all tests (unit tests only when generated without `--integrationTests`):

```cli
> dotnet test
```

When generated with `--integrationTests true`, `AspireApp.Aspire.Tests` is part of the solution, so `dotnet test` runs both unit and integration tests. To run only unit tests without starting containers:

```cli
> dotnet test AspireApp.Tests/AspireApp.Tests.csproj
```

To run only integration tests:

```cli
> dotnet test AspireApp.Aspire.Tests/AspireApp.Aspire.Tests.csproj
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

A `Dockerfile` is included in `AspireApp/` for building the API image outside of Aspire (CI/CD pipelines, Docker Compose, manual builds).

**Build the image** (run from the solution root — `templates/Aspire/AspireApp/`):

```sh
docker build -f AspireApp/Dockerfile -t myapp:latest .
```

**Run the image** (connection strings and secrets must be provided at runtime — never bake them in):

```sh
docker run -p 8080:8080 \
  -e Auth__SigningKey="<at-least-32-char-secret>" \
  -e ConnectionStrings__AspireApp-db="Server=...;Database=...;" \
  myapp:latest
```

### Security notes
- Runtime image is `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` — no shell, no package manager, non-root (`app`, UID 65532)
- HTTPS is **not** terminated in the container; configure TLS at your ingress/load balancer
- All secrets must be injected via environment variables or a secrets manager (Azure Key Vault, Kubernetes secrets, etc.)

### Aspire vs. standalone Docker
When running with `aspire run`, the AppHost uses `AddProject<Projects.AspireApp>` and Aspire handles container publishing automatically (no Dockerfile required). The `Dockerfile` is for non-Aspire workflows only.

The included GitHub Actions workflow uses Docker Buildx with GitHub Actions cache (`cache-from`/`cache-to`) and BuildKit cache-mount persistence for faster repeat builds in CI.

## Azure deployment

Infrastructure is managed with Terraform under `Infra/`. See `Infra/README.md` for setup instructions.

CI/CD deploys automatically on push to `main` via `.github/workflows/build-and-deploy.yml`.

## Application Insights

`--applicationInsights true` adds Aspire resource provisioning and `Azure.Monitor.OpenTelemetry.Profiler` (preview).

ServiceDefaults already uses OpenTelemetry: local telemetry goes to the Aspire Dashboard; setting `APPLICATIONINSIGHTS_CONNECTION_STRING` enables Azure Monitor, even without the option.

Do not also call `AddApplicationInsightsTelemetry()`; it duplicates telemetry.
