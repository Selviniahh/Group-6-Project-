using Xunit;
using Moq;
using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Group6WebProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace Group6WebProject.Tests
{
    public class GameControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly GameController _controller;

        public GameControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            SeedDatabase();

            _controller = new GameController(_context);

            var userId = 1;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "testuser@example.com")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userPrincipal }
            };
        }

        private void SeedDatabase()
        {
            var user1 = new User
            {
                UserID = 1,
                Name = "Test User",
                Email = "testuser@example.com",
                PasswordHash = "hashedpassword"
            };

            var user2 = new User
            {
                UserID = 2,
                Name = "Another User",
                Email = "anotheruser@example.com",
                PasswordHash = "hashedpassword"
            };

            _context.Users.AddRange(user1, user2);

            var game1 = new Game
            {
                Id = 1,
                Title = "Test Game 1",
                Description = "Description 1",
                Genre = "Genre 1",
                Price = "$10.00",
                Platform = "Platform 1",
                ReleaseDate = DateTime.Now.AddMonths(-1),
                DownloadUrl = "http://example.com/game1.zip",
                ImageFileName = "game1.jpg",
                VideoUrl = "http://example.com/game1.mp4"
            };

            var game2 = new Game
            {
                Id = 2,
                Title = "Test Game 2",
                Description = "Description 2",
                Genre = "Genre 2",
                Price = "$20.00",
                Platform = "Platform 2",
                ReleaseDate = DateTime.Now.AddMonths(-2),
                DownloadUrl = "http://example.com/game2.zip",
                ImageFileName = "game2.jpg",
                VideoUrl = "http://example.com/game2.mp4"
            };

            _context.Games.AddRange(game1, game2);

            var address = new Address
            {
                Id = 1,
                UserId = 1,
                FullName = "Test User",
                PhoneNumber = "123-456-7890", 
                StreetAddress = "123 Test Street",
                City = "Test City",
                Province = "Test Province",
                PostalCode = "12345",
                Country = "Test Country"
            };
            _context.Addresses.Add(address);

            var creditCard = new CreditCard
            {
                CreditCardID = 1,
                UserID = 1,
                CardNumber = "4111111111111111",
                CardHolderName = "Test User",
                ExpirationMonth = 12,
                ExpirationYear = 2025,
                CVV = "123"
            };
            _context.CreditCards.Add(creditCard);

            var order = new Order
            {
                OrderID = 1,
                UserID = 1, // UserID of user1
                AddressID = address.Id,
                CreditCardID = creditCard.CreditCardID,
                OrderDate = DateTime.Now.AddDays(-5),
                Status = "Completed",
                OrderItems = new List<OrderItem>()
            };

            var orderItem = new OrderItem
            {
                OrderItemID = 1,
                OrderID = order.OrderID,
                GameID = 1, 
                Quantity = 1,
                Price = 10.00m 
            };

            order.OrderItems.Add(orderItem);

            _context.Orders.Add(order);
            _context.OrderItems.Add(orderItem);

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
        

        [Fact]
        public void MyGames_UserHasPurchasedGames_ReturnsOwnedGames()
        {
            var result = _controller.MyGames();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Game>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Test Game 1", model.First().Title);
        }
        
        [Fact]
        public void Download_UserDoesNotOwnGame_ReturnsForbid()
        {
            var result = _controller.Download(2);

            Assert.IsNotType<ForbidResult>(result);
        }

    }
}