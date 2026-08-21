using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using MinimalApi.Core;
using MinimalApi.Data;

namespace MinimalApi.AppHost;

public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var connectionString = configuration.GetConnectionString(ConnectionStrings.DatabaseKey)
            ?? $"Server=(localdb)\\mssqllocaldb;Database={nameof(MinimalApi)}DesignTime;Trusted_Connection=True;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseAzureSql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
