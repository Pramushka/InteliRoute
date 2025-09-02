using InteliRoute.DAL.Context;
using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.DAL.Repositories.Implementations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;
using InteliRoute.Services.Integrations;
using InteliRoute.Background.Workers;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

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
builder.Services.AddScoped<IMailboxAdminRepository, MailboxAdminRepository>(); // ✅ add this
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<IDepartmentAdminRepository, DepartmentAdminRepository>();
builder.Services.AddScoped<IEmailBrowseRepository, EmailBrowseRepository>();

// Gmail integrations
builder.Services.Configure<GmailOptions>(builder.Configuration.GetSection("Gmail"));
builder.Services.AddScoped<IGmailClient, GmailClient>();
builder.Services.AddScoped<IGmailWebAuthService, GmailWebAuthService>();

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

var app = builder.Build();

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
