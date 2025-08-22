using System.Security.Claims;
using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Models.ViewModels;
using InteliRoute.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace InteliRoute.Controllers.Mvc;

public class LoginController : Controller
{
    private readonly ITenantAdminRepository _admins;

    public LoginController(ITenantAdminRepository admins) => _admins = admins;

    [HttpGet]
    public IActionResult Index(string? returnUrl = null) => View(new LoginVm { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Index(LoginVm vm, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vm.UsernameOrEmail) || string.IsNullOrWhiteSpace(vm.Password))
        {
            ModelState.AddModelError("", "Username and password are required.");
            return View(vm);
        }

        var admin = await _admins.GetByUsernameOrEmailAsync(vm.UsernameOrEmail, ct);
        if (admin is null || !PasswordHelper.VerifyPassword(vm.Password, admin.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View(vm);
        }

        // Build claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim("tenant_id", admin.TenantId.ToString()),
            new Claim(ClaimTypes.Role, admin.Role),

        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = vm.RememberMe });

        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);

        return RedirectToAction("Dashboard", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Index));
    }
}
