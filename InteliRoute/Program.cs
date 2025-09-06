using InteliRoute.DAL.Context;
using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.DAL.Repositories.Implementations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;
using InteliRoute.Services.Integrations;
using InteliRoute.Background.Workers;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.Extensions.Options;
using InteliRoute.Services.Routing;
using InteliRoute.Services.Mail;
using InteliRoute.Services.Security;
using Serilog.Context;
using Serilog.Debugging;
using Ganss.Xss;

var builder = WebApplication.CreateBuilder(args);
SelfLog.Enable(msg => Console.Error.WriteLine(msg));
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<BootstrapOptions>(builder.Configuration.GetSection("BootstrapSuperAdmin"));

// EF Core (MySQL)
var cs = builder.Configuration.GetConnectionString("MySql")
         ?? throw new InvalidOperationException("ConnectionStrings:MySql not found");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));


// Repositories
builder.Services.AddScoped<ITenantAdminRepository, TenantAdminRepository>();
builder.Services.AddScoped<ITenantMgmtRepository, TenantMgmtRepository>();
builder.Services.AddScoped<IEmailAnalyticsRepository, EmailAnalyticsRepository>();
builder.Services.AddScoped<IMailboxRepository, MailboxRepository>();
builder.Services.AddScoped<IMailboxAdminRepository, MailboxAdminRepository>(); 
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<IDepartmentAdminRepository, DepartmentAdminRepository>();
builder.Services.AddScoped<IEmailBrowseRepository, EmailBrowseRepository>();
builder.Services.AddScoped<IAppLogRepository, AppLogRepository>();

// Gmail integrations
builder.Services.Configure<GmailOptions>(builder.Configuration.GetSection("Gmail"));
builder.Services.AddScoped<IGmailClient, GmailClient>();
builder.Services.AddScoped<IGmailWebAuthService, GmailWebAuthService>();

builder.Services.Configure<RouterApiOptions>(builder.Configuration.GetSection("RouterApi"));
builder.Services.AddHttpClient<IRouterClient, RouterClient>((sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<RouterApiOptions>>().Value;
    http.BaseAddress = new Uri(opt.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSec);
});

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IMailSender, SmtpMailSender>();

// Routing worker (in addition to MailFetchWorker)
builder.Services.AddHostedService<EmailRoutingWorker>();
// Background worker
builder.Services.AddHostedService<MailFetchWorker>();

// Cookies
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.Cookie.Name = "InteliRoute.Auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<Ganss.Xss.HtmlSanitizer>(sp =>
{
    var s = new Ganss.Xss.HtmlSanitizer();
    s.AllowedSchemes.Add("data"); // inline images
    s.AllowedAttributes.Add("style");
    s.AllowedCssProperties.Add("color");
    s.AllowedCssProperties.Add("background-color");
    s.AllowedCssProperties.Add("text-align");
    s.AllowedCssProperties.Add("font-weight");
    s.AllowedCssProperties.Add("font-style");
    s.AllowedCssProperties.Add("text-decoration");
    return s;
});

var app = builder.Build();
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var cfg = app.Configuration.GetSection("BootstrapSuperAdmin").Get<BootstrapOptions>();
    if (cfg?.Enabled == true)
    {
        var tenants = scope.ServiceProvider.GetRequiredService<ITenantMgmtRepository>();
        var admins = scope.ServiceProvider.GetRequiredService<ITenantAdminRepository>();

        var allTenants = await tenants.GetAllAsync(CancellationToken.None);
        var tenant = allTenants.FirstOrDefault(t =>
            string.Equals(t.Name, cfg.TenantName, StringComparison.OrdinalIgnoreCase));

        if (tenant == null)
        {
            // CreateAsync returns Tenant (not int)
            tenant = await tenants.CreateAsync(
                cfg.TenantName,
                cfg.DomainsCsv ?? "",
                isActive: true,
                CancellationToken.None
            );
        }

        var existing = await admins.GetByUsernameOrEmailAsync(cfg.Email, CancellationToken.None);
        if (existing is null)
        {
            var hash = PasswordHelper.HashPassword(cfg.Password);
            await admins.CreateAsync(
                tenantId: tenant!.Id,
                username: cfg.Username,
                email: cfg.Email,
                passwordHash: hash,
                role: "SuperAdmin",
                isActive: true,
                CancellationToken.None
            );
        }
    }
}

app.UseSerilogRequestLogging();
// after app.UseRouting();
app.Use(async (ctx, next) =>
{
    using (Serilog.Context.LogContext.PushProperty("UserName", ctx.User?.Identity?.Name ?? "anon"))
    using (Serilog.Context.LogContext.PushProperty("TenantId", ctx.User?.Claims.FirstOrDefault(c => c.Type == "tenant")?.Value))
    {
        await next();
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Attribute routes + default route
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
