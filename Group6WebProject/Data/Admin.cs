using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Group6WebProject.Data{
public static class Admin
{
public static async Task CreateAdminUser(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string adminEmail = "admin@authenticate.com";
    string adminPassword = "ComplexAdminPassword#2024"; 
    string adminRoleName = "Admin";

    // Ensure Admin role exists
    if (await roleManager.FindByNameAsync(adminRoleName) == null)
    {
        await roleManager.CreateAsync(new IdentityRole(adminRoleName));
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    // If admin user doesn't exist, create it and normalize email
    if (adminUser == null)
    {
        adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            NormalizedEmail = userManager.NormalizeEmail(adminEmail), // Explicit normalization
            NormalizedUserName = userManager.NormalizeName(adminEmail), // Explicit normalization
            Name = "Admin User",
            Status = EnrollmentStatus.EnrollmentConfirmed
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, adminRoleName);
            Console.WriteLine("Admin user created successfully.");
        }
        else
        {
            Console.WriteLine("Failed to create Admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        // Ensure normalized email and username for existing admin user
        if (string.IsNullOrEmpty(adminUser.NormalizedEmail) || string.IsNullOrEmpty(adminUser.NormalizedUserName))
        {
            adminUser.NormalizedEmail = userManager.NormalizeEmail(adminUser.Email);
            adminUser.NormalizedUserName = userManager.NormalizeName(adminUser.Email);
            await userManager.UpdateAsync(adminUser);
            Console.WriteLine("Admin user normalized.");
        }
        else
        {
            Console.WriteLine("Admin user already normalized.");
        }
    }
}
}
    
}












