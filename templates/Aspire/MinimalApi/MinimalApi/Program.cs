using MinimalApi.Core;
using MinimalApi.Data;
using MinimalApi.Middleware;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .AddDatabase()
    .AddQAServices();

//#if (applicationInsights)
// The Application Insights Profiler captures CPU flame graphs and uploads them to App Insights.
// It activates only when APPLICATIONINSIGHTS_CONNECTION_STRING is set (auto-injected by Aspire in Azure).
// NOTE: Do NOT also call AddApplicationInsightsTelemetry() - that is the classic AI SDK and would
// double-report telemetry. The Azure Monitor OTel exporter (UseAzureMonitor in ServiceDefaults) is
// the sole App Insights integration path in this template.
builder.Services.AddServiceProfiler();
//#endif

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for cross-origin clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow any localhost origin
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // In production, restrict to specific origins from configuration
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                ?? ["https://yourdomain.com"];
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddAuthorization();

var signingKey = builder.Configuration["Auth:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    throw new InvalidOperationException("Missing required configuration value 'Auth:SigningKey'.");
}

if (signingKey.Length < 32)
{
    throw new InvalidOperationException("Configuration value 'Auth:SigningKey' must be at least 32 characters.");
}

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});

authBuilder.AddIdentityCookies(options =>
{
    options.ApplicationCookie?.Configure(cookieOptions =>
    {
        cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        if (builder.Environment.IsDevelopment())
        {
            // In development, clients may be cross-origin so we need SameSite=None
            cookieOptions.Cookie.SameSite = SameSiteMode.None;
        }
        else
        {
            // In production, clients are expected to be same-site (same eTLD+1),
            // so Lax cookies are sent on cross-origin fetch requests.
            // SameSite=None would be blocked by iOS Safari's ITP.
            cookieOptions.Cookie.SameSite = SameSiteMode.Lax;
        }
    });
});

authBuilder.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "MinimalApi",
        ValidAudience = "MinimalApi",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
    };
});

// No-op email sender for now (can be replaced with real implementation)
builder.Services.AddScoped<IEmailSender<ApplicationUser>>(sp => 
    new NoOpEmailSender<ApplicationUser>());

var app = builder.Build();

app.MapDefaultEndpoints();

// Enable CORS
app.UseCors("AllowedOrigins");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Add exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Simple no-op email sender
internal class NoOpEmailSender<TUser> : IEmailSender<TUser> where TUser : class
{
    public Task SendConfirmationLinkAsync(TUser user, string email, string confirmationLink) => Task.CompletedTask;
    public Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink) => Task.CompletedTask;
    public Task SendPasswordResetCodeAsync(TUser user, string email, string resetCode) => Task.CompletedTask;
}
