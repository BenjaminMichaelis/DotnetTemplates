using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

using MinimalApi.Data;

namespace MinimalApi.Endpoints;

internal static class AuthEndpoints
{
    internal static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync).WithName("Login");
        group.MapPost("/register", RegisterAsync).WithName("Register");
        group.MapPost("/logout", LogoutAsync).WithName("Logout").RequireAuthorization();
        group.MapGet("/user", GetCurrentUserAsync).WithName("GetCurrentUser");

        return app;
    }

    private static async Task<Results<Ok<UserInfo>, UnauthorizedHttpResult>> LoginAsync(
        LoginRequest request,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe ?? false,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(CreateUserInfo(user));
    }

    private static async Task<Results<Ok<UserInfo>, ValidationProblem>> RegisterAsync(
        RegisterRequest request,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(RegisterRequest.ConfirmPassword)] = ["Passwords do not match."]
            });
        }

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            // Automatically verify the email.
            // TODO: Implement email verification
            string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            result = await userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                return TypedResults.Ok(CreateUserInfo(user));
            }
        }

        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["registration"] = result.Errors.Select(e => e.Description).ToArray()
        });
    }

    private static async Task<NoContent> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }

    private static async Task<Ok<UserInfo>> GetCurrentUserAsync(
        ClaimsPrincipal user,
        UserManager<ApplicationUser> userManager)
    {
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return TypedResults.Ok(new UserInfo { IsAuthenticated = false });
        }

        var appUser = await userManager.GetUserAsync(user);

        return TypedResults.Ok(new UserInfo
        {
            UserId = userManager.GetUserId(user) ?? "",
            UserName = user.Identity?.Name ?? "",
            Email = appUser?.Email ?? user.Identity?.Name ?? "",
            IsAuthenticated = true
        });
    }

    private static UserInfo CreateUserInfo(ApplicationUser user) =>
        new()
        {
            UserId = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            IsAuthenticated = true
        };
}

public record LoginRequest(string Email, string Password, bool? RememberMe);
public record RegisterRequest(string Email, string Password, string ConfirmPassword);
public record UserInfo
{
    public string UserId { get; init; } = "";
    public string UserName { get; init; } = "";
    public string Email { get; init; } = "";
    public bool IsAuthenticated { get; init; }
}
