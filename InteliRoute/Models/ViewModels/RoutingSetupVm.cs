using System.ComponentModel.DataAnnotations;

namespace InteliRoute.Models.ViewModels;

public sealed class RoutingSetupVm
{
    // Left form
    [Required, Display(Name = "Routing Department")]
    public string? DepartmentName { get; set; }

    [Required, EmailAddress, Display(Name = "Routing Email")]
    public string? RoutingEmail { get; set; }

    [Display(Name = "I agree to the policies")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the policies.")]
    public bool Agree { get; set; }

    // For dropdown (pre-populated suggestions)
    public List<string> DepartmentSuggestions { get; set; } = new()
    {
        "HR Department", "MARKETING Department", "TECHNICAL Department", "FINANCE Department", "SALES Department", "SUPPORT Department"
    };

    // Right list
    public List<DepartmentRowVm> Departments { get; set; } = new();
}

public sealed class DepartmentRowVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public string RoutingEmail { get; set; } = "";
}
