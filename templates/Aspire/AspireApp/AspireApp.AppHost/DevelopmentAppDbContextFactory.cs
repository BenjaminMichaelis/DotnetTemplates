using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using AspireApp.Core;
using AspireApp.Data;

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

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseAzureSql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
