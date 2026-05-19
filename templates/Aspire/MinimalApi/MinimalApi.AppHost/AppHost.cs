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
    var sql = builder.AddSqlServer();
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
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) => app.Template.Scale.MaxReplicas = 1);

var backendMigrations = backend
    .AddEFMigrations("MinimalApi-backend-migrations", "MinimalApi.Data.ApplicationDbContext")
    .WithMigrationsProject("../MinimalApi.Data/MinimalApi.Data.csproj")
    .RunDatabaseUpdateOnStart()
    .PublishAsMigrationScript()
    .PublishAsMigrationBundle();

backend.WaitForCompletion(backendMigrations);

//#if (applicationInsights)
if (appInsights is not null)
{
    backend.WithReference(appInsights);
}
//#endif

builder.Build().Run();
