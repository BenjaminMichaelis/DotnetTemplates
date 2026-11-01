using AspireApp.Core;
using AspireApp.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspireApp.AppHost;

public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var connectionString = configuration.GetConnectionString(ConnectionStrings.DatabaseKey)
            ?? $"Server=(localdb)\\mssqllocaldb;Database={nameof(AspireApp)}DesignTime;Trusted_Connection=True;TrustServerCertificate=True";

        // Match runtime Identity options so migrations use the same schema.
        var services = new ServiceCollection();
        services.Configure<IdentityOptions>(options =>
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3);
        services.AddDbContext<ApplicationDbContext>(options => options.UseAzureSql(connectionString));
        return services.BuildServiceProvider().GetRequiredService<ApplicationDbContext>();
    }
}
