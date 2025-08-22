using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace InteliRoute.DAL.Entities;

[Table("tenant_admins")] // exact table name
public class TenantAdmin
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [Required, MaxLength(100)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("created_utc", TypeName = "datetime(6)")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required, MaxLength(50)]
    [Column("role")]
    public string Role { get; set; } = "TenantAdmin";
}
