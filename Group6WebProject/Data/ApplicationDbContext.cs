using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Group6WebProject.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<MemberPreferences> MemberPreferences { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    //Ignore pending model changes warning. For now this is commented, if during migration an error is occured, consider to uncomment this overriden method. 
    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    // }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //Convert the Status enum to a string
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
                Status = EnrollmentStatus.EnrollmentConfirmed
            });
    }
}