using Aspire.Hosting.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;

using MinimalApi.AppHost;
using MinimalApi.Core;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("MinimalApi-cae");

var docsGroup = builder.AddLogicalGroup("docs");
builder.AddAspireDocs().WithParentRelationship(docsGroup);
builder.AddMUIDocs().WithParentRelationship(docsGroup);
var authSigningKey = builder.AddParameter("auth-signing-key", secret: true);

IResourceBuilder<IResourceWithConnectionString> db;
IResourceBuilder<SqlServerServerResource>? sql = null;

//#if (applicationInsights)
// Application Insights is provisioned by Aspire in Azure and the connection string is
// injected automatically into all referencing projects as APPLICATIONINSIGHTS_CONNECTION_STRING.
// Locally, Aspire Dashboard receives all telemetry via OTLP instead.
IResourceBuilder<IResourceWithConnectionString>? appInsights = null;
//#endif

if (builder.ExecutionContext.IsPublishMode)
{
    //#if (applicationInsights)
    appInsights = builder.AddAzureApplicationInsights("appinsights");
    //#endif
    db = builder.AddAzureSqlServer().AddDatabase("MinimalApi-db");
}
else
{
    sql = builder.AddSqlServer();
    db = sql.AddSqlDatabase();

    var disableDbGate = string.Equals(
        builder.Configuration["DisableDbGate"],
        "true",
        StringComparison.OrdinalIgnoreCase);

    if (!disableDbGate)
    {
        // DBGate is a database viewer.
        var dbGate = builder.AddContainer("dbgate", "dbgate/dbgate")
            .ExcludeFromManifest()
            .ExcludeFromMcp()
            .WithExplicitStart()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithContainerName("MinimalApi-db-gate")
            .WithHttpEndpoint(targetPort: 3000)
            .WaitFor(sql)
            .WithEnvironment("CONNECTIONS", "mssql")
            .WithEnvironment("LABEL_mssql", "MS SQL")
            .WithEnvironment("SERVER_mssql", "host.docker.internal")
            .WithEnvironment("PORT_mssql", () => $"{sql.Resource.PrimaryEndpoint.Port}")
            .WithEnvironment("USER_mssql", "sa")
            .WithEnvironment("PASSWORD_mssql", sql.Resource.PasswordParameter)
            .WithEnvironment("ENGINE_mssql", "mssql@dbgate-plugin-mssql")
            .WithParentRelationship(sql)
            .WithHttpHealthCheck("/");
    }
}

var backend = builder.AddProject<Projects.MinimalApi>("MinimalApi-backend")
    .WithDependency(db, ConnectionStrings.DatabaseKey)
    .WithEnvironment("Auth__SigningKey", authSigningKey)
    .PublishAsAzureContainerApp((infra, app) => app.Template.Scale.MaxReplicas = 1);

if (!builder.ExecutionContext.IsPublishMode)
{
    backend.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");
}

// Mark endpoints as external only during publish (Azure Container Apps deployment).
// In run/test mode, Aspire's internal proxy handles routing. Calling
// WithExternalHttpEndpoints() unconditionally causes the ef-tool to inherit
// an ASPNETCORE_URLS that references MinimalApi-backend-https, which the
// ef-tool doesn't produce, causing DCP substitution failures.
if (builder.ExecutionContext.IsPublishMode)
{
    backend.WithExternalHttpEndpoints();
}

var backendMigrations = backend
    .AddEFMigrations(
        "MinimalApi-backend-migrations",
        "MinimalApi.Data.ApplicationDbContext",
        efTool => efTool.WithEnvironment("ASPNETCORE_URLS", string.Empty))
    .WithMigrationsProject("../MinimalApi.Data/MinimalApi.Data.csproj")
    .RunDatabaseUpdateOnStart()
    .PublishAsMigrationScript()
    .PublishAsMigrationBundle();

if (sql is not null)
{
    backendMigrations.WaitFor(sql);
}

backend.WaitForCompletion(backendMigrations);

//#if (applicationInsights)
if (appInsights is not null)
{
    backend.WithReference(appInsights);
}
//#endif

builder.Build().Run();
