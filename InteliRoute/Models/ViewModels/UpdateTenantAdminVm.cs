namespace InteliRoute.Models.ViewModels
{
    public class UpdateTenantAdminVm
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "TenantAdmin";
        public bool IsActive { get; set; } = true;
    }
}
