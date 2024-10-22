using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<MemberPreferences> MemberPreferences { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Review> Reviews { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Convert the Status enum to a string
        modelBuilder.Entity<User>()
            .Property(u => u.Status)
            .HasConversion<string>();

        // Seed the database with a single user as an instance in the User table
        modelBuilder.Entity<User>().HasData(
            new User()
            {
                UserID = 1,
                Name = "Default User",
                Email = "DefaultUser@example.com",
                PasswordHash = UserController.HashPassword("123"),
                Status = EnrollmentStatus.EnrollmentConfirmed,
            },
            
            //Seed the database with Admin
            new User()
            {
                UserID = 2,
                Name = "Default Admin",
                Email = "Admin@Admin.com",
                PasswordHash = UserController.HashPassword("Admin123"),
                Status = EnrollmentStatus.EnrollmentConfirmed,
                IsAdmin = true // Admin user
            });

        // Seed the default profile for the dummy user
        modelBuilder.Entity<Profile>().HasData(
            new Profile()
            {
                Id = 1,
                UserId = 1,
                Name = "Default User",
                Email = "DefaultUser@example.com",
            });
    }
}