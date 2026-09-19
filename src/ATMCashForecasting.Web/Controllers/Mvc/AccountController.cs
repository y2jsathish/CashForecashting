using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Mvc;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginRequest(string.Empty, string.Empty, null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByNameAsync(model.UserName);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Also mint a JWT so the same browser session's fetch() calls against /api/* (JWT-bearer only)
            // work without asking the user to log in twice. Picked up once by the layout's inline script.
            var roles = await _userManager.GetRolesAsync(user);
            var (token, _) = _jwtTokenService.GenerateToken(user, roles.ToList());
            TempData["AcfJwt"] = token;

            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Dashboard", "Home");
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Account locked out." : "Invalid login attempt.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
