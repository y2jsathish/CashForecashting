using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Application.Services;
using ATMCashForecasting.Domain.Entities;
using ATMCashForecasting.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditService _auditService;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _auditService = auditService;
    }

    /// <summary>Authenticates a user and returns a JWT access token for API/MVC-independent clients.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user is null || !user.IsActive)
        {
            await _auditService.LogAsync(AuditAction.LoginFailed, nameof(ApplicationUser), request.UserName, null, "User not found or inactive", null);
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            await _auditService.LogAsync(AuditAction.LoginFailed, nameof(ApplicationUser), user.Id.ToString(), null, result.ToString(), user.Id.ToString());
            return Unauthorized(new { message = result.IsLockedOut ? "Account locked out." : "Invalid credentials." });
        }

        if (user.IsMfaEnabled && string.IsNullOrWhiteSpace(request.MfaCode))
        {
            return Ok(new LoginResponse(string.Empty, DateTime.MinValue, user.UserName ?? string.Empty, Array.Empty<string>(), MfaRequired: true));
        }

        if (user.IsMfaEnabled)
        {
            var mfaValid = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.MfaCode!);
            if (!mfaValid)
            {
                return Unauthorized(new { message = "Invalid MFA code." });
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expires) = _jwtTokenService.GenerateToken(user, roles.ToList());

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync(AuditAction.Login, nameof(ApplicationUser), user.Id.ToString(), null, null, user.Id.ToString());

        return Ok(new LoginResponse(token, expires, user.UserName ?? string.Empty, roles.ToList(), MfaRequired: false));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _auditService.LogAsync(AuditAction.Logout, nameof(ApplicationUser), userId, null, null, userId);
        return NoContent();
    }
}
