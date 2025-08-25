using System.Security.Claims;

namespace InteliRoute.Services.Security;

public static class ClaimsExtensions
{
    public static bool IsSuperAdmin(this ClaimsPrincipal user)
        => user.IsInRole("SuperAdmin");

    public static int? GetTenantId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("tenant_id")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
