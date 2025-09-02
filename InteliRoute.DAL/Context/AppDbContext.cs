using InteliRoute.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Context
{
    // Make it public so EF and Web can access it
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // DbSets
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Mailbox> Mailboxes => Set<Mailbox>();
        public DbSet<EmailItem> Emails => Set<EmailItem>();
        public DbSet<EmailAttachment> EmailAttachments => Set<EmailAttachment>();
        public DbSet<Rule> Rules => Set<Rule>();
        public DbSet<RouteEvent> RouteEvents => Set<RouteEvent>();
        public DbSet<TenantAdmin> TenantAdmins => Set<TenantAdmin>();
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Tenant>().ToTable("tenants");
            b.Entity<TenantAdmin>().ToTable("tenant_admins");
            b.Entity<Department>().ToTable("departments");
            b.Entity<Mailbox>().ToTable("mailboxes");
            b.Entity<EmailItem>().ToTable("emails");
            b.Entity<EmailAttachment>().ToTable("email_attachments");
            b.Entity<Rule>().ToTable("rules");
            b.Entity<RouteEvent>().ToTable("route_events");

            // If your enums are int columns (as in the DDL), map conversions (optional in recent EF Core versions if already int):
            b.Entity<Mailbox>().Property(x => x.FetchMode).HasConversion<int>();
            b.Entity<EmailItem>().Property(x => x.PredictedIntent).HasConversion<int?>();
            b.Entity<EmailItem>().Property(x => x.RouteStatus).HasConversion<int>();
            b.Entity<Rule>().Property(x => x.ActionType).HasConversion<int>();
            b.Entity<RouteEvent>().Property(x => x.ActionType).HasConversion<int>();
            b.Entity<RouteEvent>().Property(x => x.Outcome).HasConversion<int>();
        }

    }
}
