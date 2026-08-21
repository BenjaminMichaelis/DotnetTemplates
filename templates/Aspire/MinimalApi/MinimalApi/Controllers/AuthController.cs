using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using MinimalApi.Data;

namespace MinimalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpPost("login", Name = "Login")]
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfo>> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe ?? false,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return Ok(new UserInfo
            {
                UserId = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                IsAuthenticated = true
            });
        }

        return Unauthorized(new { message = "Invalid email or password" });
    }

    [HttpPost("register", Name = "Register")]
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserInfo>> Register([FromBody] RegisterRequest request)
    {
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
                return Ok(new UserInfo
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    IsAuthenticated = true
                });
            }
        }

        return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("logout", Name = "Logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("user", Name = "GetCurrentUser")]
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
    public ActionResult<UserInfo> GetCurrentUser()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Ok(new UserInfo { IsAuthenticated = false });
        }

        return Ok(new UserInfo
        {
            UserId = userManager.GetUserId(User) ?? "",
            UserName = User.Identity?.Name ?? "",
            Email = User.Identity?.Name ?? "",
            IsAuthenticated = true
        });
    }
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
