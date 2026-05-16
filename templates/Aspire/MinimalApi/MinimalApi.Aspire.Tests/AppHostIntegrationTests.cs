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
    /// Verifies that the API service responds to health check requests.
    /// This validates that the MinimalApi service is running and reachable through the AppHost.
    /// </summary>
    [Test]
    public async Task ApiHealthCheckReturnsOk()
    {
        var httpClient = fixture.CreateHttpClient("MinimalApi-backend");
        var response = await httpClient.GetAsync("/health");
        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
    }
}
