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
    /// Verifies that the API service responds to health check requests.
    /// This validates that the MinimalApi service is running and reachable through the AppHost.
    /// </summary>
    [Test]
    public async Task ApiHealthCheckReturnsOk()
    {
        var endpoint = fixture.App.GetEndpoint("MinimalApi-backend", "http");
        await Assert.That(endpoint).IsNotNull();

        var loopbackEndpoint = new UriBuilder(endpoint)
        {
            Host = "127.0.0.1"
        }.Uri;

        Console.WriteLine($"MinimalApi-backend http endpoint: {endpoint}");
        Console.WriteLine($"MinimalApi-backend loopback endpoint: {loopbackEndpoint}");

        using var httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        })
        {
            BaseAddress = loopbackEndpoint,
            Timeout = TimeSpan.FromSeconds(30)
        };

        var response = await httpClient.GetAsync("/health");
        Console.WriteLine($"GET /health returned {(int)response.StatusCode} {response.StatusCode}");

        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
    }
}
