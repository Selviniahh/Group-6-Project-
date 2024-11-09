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
    public DbSet<GameReview> Reviews { get; set; }
    public DbSet<GameRating> Ratings { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }

    public DbSet<EventRegister> EventRegister { get; set; }

    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<CreditCard> CreditCards { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }


    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    //CC Number examples: 
    // 4111 1111 1111 1111
    // 5555 5555 5555 4444
    // 3782 822463 10005
    //6011 1111 1111 1117
    //3056 9309 0259 04
    //3530 1113 3330 0000


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
            },
            //Employee Seed
            new User()
            {
                UserID = 3,
                Name = "Default Employee",
                Email = "employee@example.com",
                PasswordHash = UserController.HashPassword("Employee123"),
                Status = EnrollmentStatus.EnrollmentConfirmed,
                IsEmployee = true
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
                Title = "Pathfinding Game",
                Description = "A Fun game to test and play pathfinding with adding obstacles",
                Genre = "Action",
                Price = "$19.99",
                Platform = "Windows",
                ReleaseDate = new DateTime(2023, 1, 1),
                DownloadUrl = "https://github.com/user-attachments/files/17656884/Pathfinding.zip",
                ImageFileName = "Pathfinding.jpg"
            },
            new Game
            {
                Id = 2,
                Title = "Enter The Gungeon",
                Description = "This is a simple shooter game trying to kill bullet mans.",
                Genre = "Adventure",
                Price = "$29.99",
                Platform = "Windows",
                ReleaseDate = new DateTime(2023, 2, 2),
                DownloadUrl = "https://github.com/user-attachments/files/17656885/ETG.zip",
                ImageFileName = "EnterTheGungeon.png"
            },
            new Game
            {
                Id = 3,
                Title = "Tile Map Game",
                Description = "Generate Tile maps selecting from right toolbar and drawing on tile map",
                Genre = "Action",
                Price = "$9.99",
                Platform = "Windows",
                ReleaseDate = new DateTime(2023, 3, 3),
                DownloadUrl = "https://github.com/user-attachments/files/17656888/TileMap.zip",
                ImageFileName = "TileMap.jpg"
            },
            new Game
            {
                Id = 4,
                Title = "Draw Circle Game",
                Description = "Draw circles, have fun with watching all the balls bouncing with each other and walls, from the right toolbar experiment with other simple physic options and have fun",
                Genre = "Action",
                Price = "$0.00",
                Platform = "Windows",
                ReleaseDate = new DateTime(2023, 4, 4),
                DownloadUrl = "https://github.com/user-attachments/files/17657333/DrawCircle.zip",
                ImageFileName = "BounceBall.jpg"
            }
        );
    }
}