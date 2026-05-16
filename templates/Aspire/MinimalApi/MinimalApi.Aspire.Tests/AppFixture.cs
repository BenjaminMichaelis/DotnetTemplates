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
}
