using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Group6WebProject.Data;
using Group6WebProject.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

var builder = WebApplication.CreateBuilder(args);

//Load .env file
Env.Load();
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Login"; // Redirect to your login page
        options.AccessDeniedPath = "/User/AccessDenied"; // Optionally configure an access denied page
    });

builder.Services.AddTransient<IReCaptchaService, ReCaptchaService>();
builder.Services.AddHttpClient();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Setup the db
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add custom services like EmailService
builder.Services.AddTransient<IEmailService, EmailService>();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/User/Login"; // Redirect to your login page
    options.AccessDeniedPath = "/User/AccessDenied"; // Optionally configure an access denied page
});

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();