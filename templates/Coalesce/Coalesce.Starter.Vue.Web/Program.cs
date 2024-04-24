using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Coalesce.Starter.Vue.Data;

using IntelliTect.Coalesce;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Console;

WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // Explicit declaration prevents ASP.NET Core from erroring if wwwroot doesn't exist at startup:
    WebRootPath = "wwwroot"
});

builder.Logging
    .AddConsole()
    // Filter out Request Starting/Request Finished noise:
    .AddFilter<ConsoleLoggerProvider>("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

#region Configure Services

IServiceCollection services = builder.Services;

services.AddDbContextFactory<AppDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), opt => opt
        .EnableRetryOnFailure()
        .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    )
    // Ignored because it interferes with the construction of Coalesce IncludeTrees via .Include()
    .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.NavigationBaseIncludeIgnored))
);

services.AddCoalesce<AppDbContext>();

services
    .AddMvc()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie();

#endregion

#region Configure HTTP Pipeline

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseViteDevelopmentServer(c => c.DevServerPort = 5002);

    app.MapCoalesceSecurityOverview("coalesce-security");

    // TODO: Dummy authentication for initial development.
    // Replace this with ASP.NET Core Identity, Windows Authentication, or some other scheme.
    // This exists only because Coalesce restricts all generated pages and API to only logged in users by default.
    app.Use(async (context, next) =>
    {
        Claim[] claims = [new Claim(ClaimTypes.Name, "developmentuser")];

        var identity = new ClaimsIdentity(claims, "dummy-auth");
        context.User = new ClaimsPrincipal(identity);

        await next.Invoke();
    });
    // End Dummy Authentication.
}

app.UseAuthentication();
app.UseAuthorization();

Regex containsFileHashRegex = ContainsFileHash();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // vite puts 8-hex-char hashes before the file extension.
        // Use this to determine if we can send a long-term cache duration.
        if (containsFileHashRegex.IsMatch(ctx.File.Name))
        {
            ctx.Context.Response.GetTypedHeaders().CacheControl = new() { Public = true, MaxAge = TimeSpan.FromDays(30) };
        }
    }
});

// For all requests that aren't to static files, disallow caching by default.
// Individual endpoints may override this.
app.Use(async (context, next) =>
{
    context.Response.GetTypedHeaders().CacheControl = new() { NoCache = true, NoStore = true };
    await next();
});

app.MapDefaultControllerRoute();

// API fallback to prevent serving SPA fallback to 404 hits on API endpoints.
#pragma warning disable ASP0018 // Unused route parameter
app.Map("/api/{**any}", () => Results.NotFound());
#pragma warning restore ASP0018 // Unused route parameter

app.MapFallbackToController("Index", "Home");

#endregion

#region Launch

// Initialize/migrate database.
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider serviceScope = scope.ServiceProvider;

    // Run database migrations.
    using AppDbContext db = serviceScope.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();

partial class Program
{
    [GeneratedRegex(@"[.-][0-9a-zA-Z-_]{8}\.[^\.]*$", RegexOptions.Compiled)]
    private static partial Regex ContainsFileHash();
}
#endregion
