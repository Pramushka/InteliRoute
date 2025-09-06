using InteliRoute.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace InteliRoute.DAL.Context
{
    // Make it public so EF and Web can access it
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Mailbox> Mailboxes => Set<Mailbox>();
        public DbSet<EmailItem> Emails => Set<EmailItem>();
        public DbSet<EmailAttachment> EmailAttachments => Set<EmailAttachment>();
        public DbSet<Rule> Rules => Set<Rule>();
        public DbSet<RouteEvent> RouteEvents => Set<RouteEvent>();
        public DbSet<TenantAdmin> TenantAdmins => Set<TenantAdmin>();
        public DbSet<AppLog> AppLogs => Set<AppLog>();
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
            b.Entity<AppLog>().ToTable("app_logs");


            b.Entity<Mailbox>().Property(x => x.FetchMode).HasConversion<int>();
            b.Entity<EmailItem>().Property(x => x.PredictedIntent).HasConversion<int?>();
            b.Entity<EmailItem>().Property(x => x.RouteStatus).HasConversion<int>();
            b.Entity<Rule>().Property(x => x.ActionType).HasConversion<int>();
            b.Entity<RouteEvent>().Property(x => x.ActionType).HasConversion<int>();
            b.Entity<RouteEvent>().Property(x => x.Outcome).HasConversion<int>();

            b.Entity<AppLog>(eb =>
            {
                eb.ToTable("app_logs");
                eb.HasKey(x => x.Id);

                eb.Property(x => x.Timestamp).HasColumnName("TimestampUtc");
                eb.Property(x => x.Level).HasColumnName("Level");
                eb.Property(x => x.Message).HasColumnName("Message");
                eb.Property(x => x.MessageTemplate).HasColumnName("MessageTemplate");
                eb.Property(x => x.Exception).HasColumnName("Exception");
                eb.Property(x => x.Properties).HasColumnName("Properties");
                eb.Property(x => x.Source).HasColumnName("Source");
                eb.Property(x => x.TenantId).HasColumnName("TenantId");
                eb.Property(x => x.UserName).HasColumnName("UserName");
                eb.Property(x => x.LogLevel).HasColumnName("LogLevel");
                // These don't exist in the table:
                eb.Ignore(x => x.MailboxId);
                eb.Ignore(x => x.EmailId);
            });

        }
      
    }
}
