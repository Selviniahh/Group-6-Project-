using Microsoft.EntityFrameworkCore;
using Group6WebProject.Data;
using Group6WebProject.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Setup the db 
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddTransient<IEmailService, EmailService>();

//Set the default authentication scheme to cookies




builder.Services.AddIdentity<User, IdentityRole>()  // User is your custom user class
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/User/Login";  // Ensure that this points to your login page
    options.AccessDeniedPath = "/User/AccessDenied";  // Optionally, add an access denied page
    options.Cookie.Name = "Identity.Application";  // This sets the correct cookie scheme
});
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6; // Minimum password length
});

builder.Logging.AddFilter("Microsoft.AspNetCore.Identity", LogLevel.Debug);
var app = builder.Build();

// Configure the HTTP request pipeline.
// if (!app.Environment.IsDevelopment())
// {
//     app.UseExceptionHandler("/Home/Error");
//     // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//     app.UseHsts();
// }
// 5. Seed the Admin User and Role


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await Admin.Initialize(services); 
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during admin seeding.");
    }
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}"); //I am changing this already existing line

app.Run();

//This is extra two line that I am changing 
//Chanelle's first commit
//KP
//Vidhi's Commit