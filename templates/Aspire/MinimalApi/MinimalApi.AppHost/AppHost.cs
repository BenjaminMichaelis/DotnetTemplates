using MinimalApi.AppHost;
using MinimalApi.Core;

using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("MinimalApi-cae");

var docsGroup = builder.AddLogicalGroup("docs");
builder.AddAspireDocs().WithParentRelationship(docsGroup);
builder.AddMUIDocs().WithParentRelationship(docsGroup);

IResourceBuilder<IResourceWithConnectionString> db;

//#if (applicationInsights)
// Application Insights is provisioned by Aspire in Azure and the connection string is
// injected automatically into all referencing projects as APPLICATIONINSIGHTS_CONNECTION_STRING.
// Locally, Aspire Dashboard receives all telemetry via OTLP instead.
var appInsights = builder.AddAzureApplicationInsights("appinsights");
//#endif

if (builder.ExecutionContext.IsPublishMode)
{
    db = builder.AddAzureSqlServer().AddDatabase("MinimalApi-db");
}
else
{
    var sql = builder.AddSqlServer();
    db = sql.AddSqlDatabase();

    //DBGate is a database viewer
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
        .WithHttpHealthCheck("/")
        ;
}

var backend = builder.AddProject<Projects.MinimalApi>("MinimalApi-backend")
    .WithDependency(db, ConnectionStrings.DatabaseKey)
    //#if (applicationInsights)
    .WithReference(appInsights)
    //#endif
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) => app.Template.Scale.MaxReplicas = 1);

if (builder.ExecutionContext.IsPublishMode)
{
    // Enable migrations on startup for Azure deployments
    // Applying migrations on startup is not recommended for production scenarios.
    // See: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli&WT.mc_id=DT-MVP-5003472
    backend.WithEnvironment("RunMigrationsOnStartup", "true");
}

builder.Build().Run();
