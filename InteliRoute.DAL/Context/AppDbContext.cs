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

            // Table names (optional—EF will pluralize by default)
            b.Entity<Tenant>().ToTable("tenants");
            b.Entity<Department>().ToTable("departments");
            b.Entity<Mailbox>().ToTable("mailboxes");
            b.Entity<EmailItem>().ToTable("emails");
            b.Entity<EmailAttachment>().ToTable("email_attachments");
            b.Entity<Rule>().ToTable("rules");
            b.Entity<RouteEvent>().ToTable("route_events");
            b.Entity<TenantAdmin>().ToTable("tenant_admins");


            // Email unique constraint: (mailbox, external message id) 
            b.Entity<EmailItem>()
                .HasIndex(e => new { e.MailboxId, e.ExternalMessageId })
                .IsUnique();

            // Useful indexes
            b.Entity<EmailItem>()
                .HasIndex(e => new { e.TenantId, e.ReceivedUtc });

            b.Entity<EmailItem>()
                .HasIndex(e => e.RouteStatus);

            b.Entity<Rule>()
                .HasIndex(r => new { r.TenantId, r.Priority });

            // Relationships
            b.Entity<EmailItem>()
                .HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<EmailItem>()
                .HasOne(e => e.Mailbox)
                .WithMany()
                .HasForeignKey(e => e.MailboxId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<EmailItem>()
                .HasOne(e => e.RoutedDepartment)
                .WithMany()
                .HasForeignKey(e => e.RoutedDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            b.Entity<RouteEvent>()
                .HasOne(x => x.EmailItem)
                .WithMany(e => e.RouteEvents)
                .HasForeignKey(x => x.EmailItemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<RouteEvent>()
                .HasOne(x => x.Rule)
                .WithMany()
                .HasForeignKey(x => x.RuleId)
                .OnDelete(DeleteBehavior.SetNull);

            b.Entity<Mailbox>().Property(x => x.Address).HasMaxLength(320);
            b.Entity<Department>().Property(x => x.RoutingEmail).HasMaxLength(320);
            b.Entity<EmailItem>().Property(x => x.ExternalMessageId).HasMaxLength(128);
            b.Entity<EmailItem>().Property(x => x.ThreadId).HasMaxLength(128);
            b.Entity<EmailItem>().Property(x => x.From).HasMaxLength(500);
            b.Entity<EmailItem>().Property(x => x.To).HasMaxLength(500);
            b.Entity<EmailItem>().Property(x => x.Subject).HasMaxLength(500);
        }
    }
}
