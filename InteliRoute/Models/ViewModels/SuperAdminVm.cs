using System.ComponentModel.DataAnnotations;

namespace InteliRoute.Models.ViewModels;

public class TenantRowVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string DomainsCsv { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class TenantAdminRowVm
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "TenantAdmin";
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class NewTenantVm
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Display(Name = "Domains (CSV)")]
    public string DomainsCsv { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

public class UpdateTenantVm
{
    [Required]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Display(Name = "Domains (CSV)")]
    public string DomainsCsv { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

public class NewTenantAdminVm
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = "";

    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = "";

    [Required, MinLength(6)]
    public string Password { get; set; } = "";

    [MaxLength(50)]
    public string Role { get; set; } = "TenantAdmin";

    public bool IsActive { get; set; } = true;
}

public class ResetAdminPasswordVm
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; }

    [Required, MinLength(6)]
    public string NewPassword { get; set; } = "";
}

public class SuperAdminIndexVm
{
    public IReadOnlyList<TenantRowVm> Tenants { get; set; } = Array.Empty<TenantRowVm>();
    public int? SelectedTenantId { get; set; }

    public IReadOnlyList<TenantAdminRowVm> TenantAdmins { get; set; } = Array.Empty<TenantAdminRowVm>();

    // Forms
    public NewTenantVm NewTenant { get; set; } = new();
    public UpdateTenantVm? EditTenant { get; set; } // shown when a tenant selected
    public NewTenantAdminVm NewAdmin { get; set; } = new();
}
