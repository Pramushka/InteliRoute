using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace InteliRoute.DAL.Entities;

[Table("tenant_admins")] // exact table name
public class TenantAdmin
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int TenantId { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    [Required, MaxLength(50)]
    public string Role { get; set; } = "TenantAdmin";
}
