using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Frontend.Dtos;
using Mesa_Mohloane_Frontend.Services.Api;
using System.Security.Claims;

namespace Mesa_Mohloane_Frontend.Controllers;

public class AuthController : Controller
{
    private readonly IAuthApiService _authApi;

    public AuthController(IAuthApiService authApi)
        => _authApi = authApi;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDashboard(User.FindFirstValue(ClaimTypes.Role));

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestDto model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var (ok, data, error) = await _authApi.LoginAsync(model);

        if (!ok || data is null)
        {
            ViewData["Error"] = error ?? "Invalid email or password.";
            return View(model);
        }

        await SignInAsync(data);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToDashboard(data.Role);
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDashboard(User.FindFirstValue(ClaimTypes.Role));

        await LoadPublicRolesAsync();
        return View(new RegisterRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestDto model)
    {
        await LoadPublicRolesAsync();

        if (model.RoleId == Guid.Empty)
            ModelState.AddModelError(nameof(model.RoleId), "Please choose whether you are registering as a Citizen or Contractor.");

        if (!ModelState.IsValid)
            return View(model);

        var (ok, data, error) = await _authApi.RegisterAsync(model);

        if (!ok || data is null)
        {
            ViewData["Error"] = error ?? "Registration failed. Please check your details and try again.";
            return View(model);
        }

        await SignInAsync(data);
        return RedirectToDashboard(data.Role);
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
        => RedirectToAction(nameof(Login), new { returnUrl });

    private async Task LoadPublicRolesAsync()
    {
        var roles = await _authApi.GetPublicRolesAsync() ?? new List<RoleDto>();

        ViewData["Roles"] = roles
            .Where(r =>
                string.Equals(r.Name, "Citizen", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Name, "Contractor", StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Name)
            .ToList();
    }

    private async Task SignInAsync(AuthResponseDto data)
    {
        HttpContext.Session.SetString("jwt_token", data.Token);
        HttpContext.Session.SetString("user_role", data.Role);
        HttpContext.Session.SetString("user_id", data.UserId.ToString());
        HttpContext.Session.SetString("user_name", data.FullName);
        HttpContext.Session.SetString("user_email", data.Email);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, data.UserId.ToString()),
            new(ClaimTypes.Name, data.FullName),
            new(ClaimTypes.Email, data.Email),
            new(ClaimTypes.Role, data.Role),
            new("jwt_token", data.Token)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = data.ExpiresAt.HasValue
                    ? new DateTimeOffset(data.ExpiresAt.Value)
                    : DateTimeOffset.UtcNow.AddHours(8)
            });
    }

    private IActionResult RedirectToDashboard(string? role) => role switch
    {
        "Administrator" => RedirectToAction("Dashboard", "Admin"),
        "Contractor" => RedirectToAction("Dashboard", "Contractor"),
        "Inspector" => RedirectToAction("Dashboard", "Inspector"),
        "Auditor" => RedirectToAction("Dashboard", "Inspector"),
        _ => RedirectToAction("Dashboard", "Citizen")
    };
}