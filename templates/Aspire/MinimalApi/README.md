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

## Container / Docker

A `Dockerfile` is included in `ReactApp/` for building the API image outside of Aspire (CI/CD pipelines, Docker Compose, manual builds).

**Build the image** (run from the solution root — `templates/Aspire/MinimalApi/`):

```sh
docker build -f ReactApp/Dockerfile -t myapp:latest .
```

**Run the image** (connection strings and secrets must be provided at runtime — never bake them in):

```sh
docker run -p 8080:8080 \
  -e Auth__SigningKey="<at-least-32-char-secret>" \
  -e ConnectionStrings__ReactApp-db="Server=...;Database=...;" \
  myapp:latest
```

### Security notes
- Runtime image is `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` — no shell, no package manager, non-root (`app`, UID 65532)
- HTTPS is **not** terminated in the container; configure TLS at your ingress/load balancer
- All secrets must be injected via environment variables or a secrets manager (Azure Key Vault, Kubernetes secrets, etc.)

### Aspire vs. standalone Docker
When running with `aspire run`, the AppHost uses `AddProject<Projects.ReactApp>` and Aspire handles container publishing automatically (no Dockerfile required). The `Dockerfile` is for non-Aspire workflows only.

## Azure deployment

Infrastructure is managed with Terraform under `Infra/`. See `Infra/README.md` for setup instructions.

CI/CD deploys automatically on push to `main` via `.github/workflows/build-and-deploy.yml`.
