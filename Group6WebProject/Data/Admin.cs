
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Group6WebProject.Data
{
    public static class Admin
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // Ensure the Admin role exists
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var adminEmail = "admin@authenticate.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            // Check if admin user already exists
            if (adminUser == null)
            {
                // Create a new admin user with all required fields
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Name = "Admin User",
                    Status = EnrollmentStatus.EnrollmentConfirmed
                };

                // Create the admin user with the correct password
                var result = await userManager.CreateAsync(adminUser, "ComplexAdminPassword#2024");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("Admin user created successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to create Admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // User already exists
                Console.WriteLine("Admin user already exists.");
            }
        }
    }
}