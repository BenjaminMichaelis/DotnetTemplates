using TUnit.Aspire;

namespace MinimalApi.Aspire.Tests;

/// <summary>
/// Custom fixture for MinimalApi AppHost integration tests.
/// Provides lifecycle management and resource orchestration for testing the distributed application.
/// </summary>
public class AppFixture : AspireFixture<Projects.MinimalApi_AppHost>
{
    private const string SigningKey = "ThisIsATemplateDevelopmentSigningKeyAtLeast32Chars";

    protected override string[] Args =>
    [
        "--DisableDbGate=true",
        $"--Parameters:auth-signing-key={SigningKey}"
    ];

    /// <summary>
    /// Timeout for waiting on resources to become healthy.
    /// Adjust this based on your infrastructure startup time.
    /// </summary>
    protected override TimeSpan ResourceTimeout => TimeSpan.FromMinutes(2);

    /// <summary>
    /// Only wait for the SQL server and API backend to be healthy.
    /// The migration resource (MinimalApi-backend-migrations) is a one-shot resource that
    /// transitions to Finished (not Healthy) on success. Waiting for it to be healthy would
    /// always fail. The backend already waits for the migration to complete via
    /// WaitForCompletion, so if migration fails the backend will not become healthy either.
    /// </summary>
    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.Named;

    protected override IEnumerable<string> ResourcesToWaitFor() =>
        ["MinimalApi-sql", "MinimalApi-backend"];
}
