using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Group6WebProject.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<MemberPreferences> MemberPreferences { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }

    
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
        // Create relationship for friends and family list 
        modelBuilder.Entity<User>()
            .HasMany(u => u.FriendsAndFamily)
            .WithMany()
            .UsingEntity(j => j.ToTable("UserFriendsAndFamily"));
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
                Gender = Gender.PreferNotToSay,
                BirthDate = new DateTime(2000, 1, 1),
            });

        modelBuilder.Entity<Game>().HasData(
            new Game
            {
                Id = 1,
                Title = "Game One",
                Description = "First Game",
                Genre = "Action",
                Price = "$19.99",
                Platform = "PC",
                ReleaseDate = new DateTime(2021, 1, 1)
            },
            new Game
            {
                Id = 2,
                Title = "Game Two",
                Description = "Second Game",
                Genre = "Adventure",
                Price = "$29.99",
                Platform = "PlayStation",
                ReleaseDate = new DateTime(2021, 6, 1)
            },
            new Game
            {
                Id = 3,
                Title = "Game Three",
                Description = "Third Game",
                Genre = "Action",
                Price = "$19.99",
                Platform = "PC",
                ReleaseDate = new DateTime(2023, 1, 1)
            }
        );
    }
}