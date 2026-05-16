using TUnit.Aspire;

namespace MinimalApi.Aspire.Tests;

/// <summary>
/// Custom fixture for MinimalApi AppHost integration tests.
/// Provides lifecycle management and resource orchestration for testing the distributed application.
/// </summary>
public class AppFixture : AspireFixture<Projects.MinimalApi_AppHost>
{
    public AppFixture()
    {
        Environment.SetEnvironmentVariable(
            "Auth__SigningKey",
            "ThisIsATemplateDevelopmentSigningKeyAtLeast32Chars");
        Environment.SetEnvironmentVariable("DisableDbGate", "true");
    }

    /// <summary>
    /// Timeout for waiting on resources to become healthy.
    /// Adjust this based on your infrastructure startup time.
    /// </summary>
    protected override TimeSpan ResourceTimeout => TimeSpan.FromMinutes(2);
}
