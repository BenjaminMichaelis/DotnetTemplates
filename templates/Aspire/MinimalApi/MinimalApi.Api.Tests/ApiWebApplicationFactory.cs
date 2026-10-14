using TUnit.AspNetCore;

namespace MinimalApi.Api.Tests;

public sealed class ApiWebApplicationFactory : TestWebApplicationFactory<Program>
{
    protected override void ConfigureStartupConfiguration(IConfigurationBuilder configuration)
    {
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Server=localhost;Database=MinimalApiTests;TrustServerCertificate=True",
            ["Auth:SigningKey"] = "ThisIsATestSigningKeyAtLeast32CharactersLong"
        });
    }
}
