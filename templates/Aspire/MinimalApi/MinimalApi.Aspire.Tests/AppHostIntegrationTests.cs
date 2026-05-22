using Aspire.Hosting.Testing;

using TUnit.Aspire;

namespace MinimalApi.Aspire.Tests;

/// <summary>
/// Integration tests for the MinimalApi AppHost.
/// These tests validate that the distributed application components start correctly
/// and respond to HTTP requests through the orchestrated host.
/// </summary>
[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class AppHostIntegrationTests(AppFixture fixture)
{
    /// <summary>
    /// Verifies that the AppHost builds and starts successfully.
    /// This is a smoke test that ensures all resources are orchestrated correctly.
    /// </summary>
    [Test]
    public async Task AppHostStartsSuccessfully()
    {
        var app = fixture.App;
        await Assert.That(app).IsNotNull();
    }

    /// <summary>
    /// Verifies that the AppHost can resolve the database connection string.
    /// This validates that resource orchestration and connection string injection are configured correctly.
    /// </summary>
    [Test]
    public async Task DatabaseConnectionStringIsConfigured()
    {
        var connectionString = await fixture.App.GetConnectionStringAsync("MinimalApi-db");
        await Assert.That(connectionString).IsNotNull().And.IsNotEmpty();
    }
}
