using System.Security.Claims;
using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Models.ViewModels;
using InteliRoute.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting; // IsDevelopment

namespace InteliRoute.Controllers.Mvc;

public class LoginController : Controller
{
    private readonly ITenantAdminRepository _admins;
    private readonly IHostEnvironment _env;

    private const string DevUserOrEmail = "super@inteli.local";   
    private const string DevPassword = "Super!123";          
    private const int DevTenantId = 1;                     
    private const int DevAdminId = -1;                  
    private const string DevRole = "SuperAdmin";          

    public LoginController(ITenantAdminRepository admins, IHostEnvironment env)
    {
        _admins = admins;
        _env = env;
    }

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

        //  DEV-ONLY BACKDOOR
        if (_env.IsDevelopment() && IsDevSuperAdmin(vm.UsernameOrEmail, vm.Password))
        {
            var claims = BuildClaims(
                id: DevAdminId,
                username: "SuperAdmin",
                email: DevUserOrEmail,
                tenantId: DevTenantId,
                role: DevRole
            );

            await SignInAsync(claims, vm.RememberMe);

            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);

            return RedirectToAction("Dashboard", "Home");
        }

        //  Normal repo-backed auth
        var admin = await _admins.GetByUsernameOrEmailAsync(vm.UsernameOrEmail, ct);
        if (admin is null || !PasswordHelper.VerifyPassword(vm.Password, admin.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View(vm);
        }

        var normalClaims = BuildClaims(
            id: admin.Id,
            username: admin.Username,
            email: admin.Email,
            tenantId: admin.TenantId,
            role: admin.Role
        );

        await SignInAsync(normalClaims, vm.RememberMe);

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

    // --------- helpers ----------
    private static bool IsDevSuperAdmin(string userOrEmail, string password)
        => string.Equals(userOrEmail.Trim(), DevUserOrEmail, StringComparison.OrdinalIgnoreCase)
           && password == DevPassword;

    // LoginController.cs
    private static List<Claim> BuildClaims(int id, string username, string email, int tenantId, string role)
        => new()
    {
    new Claim(ClaimTypes.NameIdentifier, id.ToString()),
    new Claim(ClaimTypes.Name, username),
    new Claim(ClaimTypes.Email, email),
    new Claim("tenant", tenantId.ToString()),  
    new Claim(ClaimTypes.Role, role),
    };


    private async Task SignInAsync(IEnumerable<Claim> claims, bool remember)
    {
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = remember });
    }
}
