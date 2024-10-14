using Microsoft.EntityFrameworkCore;
using Group6WebProject.Data;
using Group6WebProject.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Identity with custom options (like password policies)
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

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

// Ensure the admin user and role are seeded
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<User>>();

    try
    {
        await Admin.CreateAdminUser(services); // This ensures the admin user and role are created

        // Normalize emails for all users
        var users = userManager.Users.ToList();
        foreach (var user in users)
        {
            if (string.IsNullOrEmpty(user.NormalizedEmail))
            {
                user.NormalizedEmail = userManager.NormalizeEmail(user.Email);
                await userManager.UpdateAsync(user);
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the admin user.");
    }
}

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();  // Ensure authentication is enabled
app.UseAuthorization();   // Ensure authorization is enabled

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
//This is extra two line that I am changing 
//Chanelle's first commit
//KP
//Vidhi's Commit