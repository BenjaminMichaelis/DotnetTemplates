using Aspire.Hosting.Testing;
using System.Text.Json.Nodes;

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
    /// Verifies that the backend is reachable through Aspire orchestration.
    /// The liveness endpoint proves HTTP reachability without coupling the test to database readiness checks.
    /// </summary>
    [Test]
    public async Task BackendAliveEndpointReturnsSuccess()
    {
        using var client = fixture.CreateHttpClient("MinimalApi-backend", "http");

        using var response = await client.GetAsync("/alive");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
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

    [Test]
    public async Task OpenApiDocumentIncludesOperationIdsAndTypedSchemas()
    {
        using var client = fixture.CreateHttpClient("MinimalApi-backend", "http");

        var document = await client.GetFromJsonAsync<JsonObject>("/openapi/v1.json");

        await Assert.That(document).IsNotNull();

        var paths = document!["paths"]!.AsObject();

        foreach (var path in paths)
        {
            foreach (var operation in path.Value!.AsObject())
            {
                var operationId = operation.Value!["operationId"]?.GetValue<string>();
                await Assert.That(operationId).IsNotNull().And.IsNotEmpty();
            }
        }

        var loginOperation = GetOperation(paths, "/api/auth/login", "post");
        var loginSchema = loginOperation["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["$ref"]?.GetValue<string>();
        await Assert.That(loginSchema).IsEqualTo("#/components/schemas/UserInfo");

        var registerOperation = GetOperation(paths, "/api/auth/register", "post");
        var registerSchema = registerOperation["responses"]!["400"]!["content"]!["application/problem+json"]!["schema"]!["$ref"]?.GetValue<string>();
        await Assert.That(registerSchema).IsEqualTo("#/components/schemas/HttpValidationProblemDetails");

        var roomsOperation = GetOperation(paths, "/api/rooms/", "get");
        var roomItemsSchema = roomsOperation["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["items"]!["$ref"]?.GetValue<string>();
        await Assert.That(roomItemsSchema).IsEqualTo("#/components/schemas/RoomDto");
    }

    private static JsonObject GetOperation(JsonObject paths, string path, string method) =>
        paths[path]?[method]?.AsObject()
        ?? throw new InvalidOperationException($"OpenAPI operation '{method.ToUpperInvariant()} {path}' was not found.");
}
