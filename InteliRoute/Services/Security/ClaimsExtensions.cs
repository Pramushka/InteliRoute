using System.Security.Claims;

namespace InteliRoute.Services.Security;

public static class ClaimsExtensions
{
    public static bool IsSuperAdmin(this ClaimsPrincipal user)
        => user.IsInRole("SuperAdmin");

    public static int? GetTenantId(this ClaimsPrincipal user)
    {
        if (user is null) return null;

        // Accept any of these names
        var raw =
            user.FindFirst("tenant")?.Value ??
            user.FindFirst("tenant_id")?.Value ??
            user.FindFirst("TenantId")?.Value;

        return int.TryParse(raw, out var id) ? id : (int?)null;
    }

    // Optional helper if you like explicit checks:
    public static bool TryGetTenantId(this ClaimsPrincipal user, out int tenantId)
    {
        tenantId = 0;
        var id = user.GetTenantId();
        if (id is null) return false;
        tenantId = id.Value;
        return true;
    }
}
