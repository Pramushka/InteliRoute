using InteliRoute.DAL.Context;
using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.DAL.Repositories.Implementations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog ----
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

// ---- MVC (Razor Views) ----
builder.Services.AddControllersWithViews();

// ---- EF Core (MySQL via Pomelo) ----
var cs = builder.Configuration.GetConnectionString("MySql")
         ?? throw new InvalidOperationException("ConnectionStrings:MySql not found");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// ---- Repositories ----
builder.Services.AddScoped<ITenantAdminRepository, TenantAdminRepository>();
// builder.Services.AddScoped<IEmailRepository, EmailRepository>();
// builder.Services.AddScoped<IRuleRepository, RuleRepository>();
// builder.Services.AddScoped<ITenantRepository, TenantRepository>();
// builder.Services.AddScoped<IMailboxRepository, MailboxRepository>();

// ---- Background workers (optional; later) ----
// builder.Services.AddHostedService<MailFetchWorker>();

// ---- Cookie authentication (no sessions) ----
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

// ---- Middleware pipeline ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // <— IMPORTANT: before Authorization
app.UseAuthorization();

// Attribute-routed APIs
app.MapControllers();

// MVC default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
