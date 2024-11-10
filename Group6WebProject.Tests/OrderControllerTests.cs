using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Group6WebProject.Data;
using Group6WebProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Group6WebProject.Tests
{
    //7 Tests
    public class OrderControllerTests
    {
        private readonly OrderController _controller;
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;

        public OrderControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            // Mock the email service
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _emailService = emailServiceMock.Object;

            _controller = new OrderController(_dbContext, _emailService);

            var testUserId = 1;
            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, testUserId.ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "testuser@example.com"),
            }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = mockUser }
            };

            SeedTestData(testUserId);
        }

        private void SeedTestData(int testUserId)
        {
            // Add test user
            var testUser = new User
            {
                UserID = testUserId,
                Name = "Test User",
                Email = "testuser@example.com",
                PasswordHash = "hashedpassword"
            };
            _dbContext.Users.Add(testUser);

            var testGame = new Game
            {
                Id = 1,
                Title = "Test Game",
                Description = "This is a test game description.",
                Genre = "Test Genre",
                Price = "$19.99",
                Platform = "Test Platform",
                ReleaseDate = DateTime.Now,
                DownloadUrl = "https://example.com/download",
                ImageFileName = "testgame.jpg",
                VideoUrl = "https://example.com/video"
            };
            _dbContext.Games.Add(testGame);

            var testCart = new Cart
            {
                CartID = 1,
                UserID = testUserId,
                User = testUser,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        CartItemID = 1,
                        GameID = testGame.Id,
                        Game = testGame,
                        Quantity = 1
                    }
                }
            };
            _dbContext.Carts.Add(testCart);

            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Checkout_Succeeds_WhenUserHasCartItemsAddressAndCreditCard()
        {
            // Arrange
            var testUserId = 1;

            // Add a test credit card
            var testCreditCard = new CreditCard
            {
                CreditCardID = 1,
                UserID = testUserId,
                CardNumber = "4111111111111111",
                CardHolderName = "Test User",
                ExpirationMonth = 12,
                ExpirationYear = 2025,
                CVV = "123"
            };
            _dbContext.CreditCards.Add(testCreditCard);

            var testAddress = new Address
            {
                Id = 1,
                UserId = testUserId,
                FullName = "Test User",
                PhoneNumber = "123-456-7890", // Include PhoneNumber
                StreetAddress = "123 Test Street",
                City = "Testville",
                Province = "Test State",
                PostalCode = "12345",
                Country = "Testland"
            };
            _dbContext.Addresses.Add(testAddress);

            _dbContext.SaveChanges();

            var result = await _controller.Checkout(creditCardId: testCreditCard.CreditCardID, addressId: testAddress.Id);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("OrderDetails", redirectResult.ActionName);

            // Verify that the order was created
            var order = await _dbContext.Orders
                .Include(o => o.User)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.UserID == testUserId);
            Assert.NotNull(order);
            Assert.Equal("Pending", order.Status);
            Assert.Equal(testAddress.Id, order.AddressID);
            Assert.Equal(testCreditCard.CreditCardID, order.CreditCardID);
            Assert.NotNull(order.User);
            Assert.NotNull(order.ShippingAddress);
        }

        // Test 2: Checkout fails when user has no credit card
        [Fact]
        public async Task Checkout_Fails_WhenUserHasNoCreditCard()
        {
            var testUserId = 1;
            var testAddress = new Address
            {
                Id = 2,
                UserId = testUserId,
                FullName = "Test User",
                PhoneNumber = "123-456-7890",
                StreetAddress = "456 Test Avenue",
                City = "Test City",
                Province = "Test Province",
                PostalCode = "67890",
                Country = "Testland"
            };
            _dbContext.Addresses.Add(testAddress);

            _dbContext.SaveChanges();

            var result = await _controller.Checkout(creditCardId: 0, addressId: testAddress.Id);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, m => m.Key == "" && m.Value.Errors.Any(e => e.ErrorMessage == "Invalid credit card selected."));

            Assert.NotNull(viewResult.ViewData["CreditCards"]);
            Assert.NotNull(viewResult.ViewData["Addresses"]);
        }

        [Fact]
        public async Task Checkout_Fails_WhenUserHasNoAddress()
        {
            var testUserId = 1;

            var testCreditCard = new CreditCard
            {
                CreditCardID = 1,
                UserID = testUserId,
                CardNumber = "4111111111111111",
                CardHolderName = "Test User",
                ExpirationMonth = 12,
                ExpirationYear = 2025,
                CVV = "123"
            };
            _dbContext.CreditCards.Add(testCreditCard);
            await _dbContext.SaveChangesAsync();

            var result = await _controller.Checkout(creditCardId: testCreditCard.CreditCardID, addressId: 0);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, m => m.Key == "" && m.Value.Errors.Any(e => e.ErrorMessage == "Invalid shipping address selected."));

            Assert.NotNull(viewResult.ViewData["CreditCards"]);
            Assert.NotNull(viewResult.ViewData["Addresses"]);
        }

        [Fact]
        public async Task Checkout_Fails_WhenInvalidCreditCardIdProvided()
        {
            var testUserId = 1;

            var testAddress = new Address
            {
                Id = 1,
                UserId = testUserId,
                FullName = "Test User",
                PhoneNumber = "123-456-7890",
                StreetAddress = "123 Test Street",
                City = "Testville",
                Province = "Test State",
                PostalCode = "12345",
                Country = "Testland"
            };
            _dbContext.Addresses.Add(testAddress);
            await _dbContext.SaveChangesAsync();

            var invalidCreditCardId = 999;
            var result = await _controller.Checkout(creditCardId: invalidCreditCardId, addressId: testAddress.Id);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, m => m.Key == "" && m.Value.Errors.Any(e => e.ErrorMessage == "Invalid credit card selected."));

            Assert.NotNull(viewResult.ViewData["CreditCards"]);
            Assert.NotNull(viewResult.ViewData["Addresses"]);
        }

        [Fact]
        public async Task Checkout_Fails_WhenInvalidAddressIdProvided()
        {
            var testUserId = 1;

            var testCreditCard = new CreditCard
            {
                CreditCardID = 1,
                UserID = testUserId,
                CardNumber = "4111111111111111",
                CardHolderName = "Test User",
                ExpirationMonth = 12,
                ExpirationYear = 2025,
                CVV = "123"
            };
            _dbContext.CreditCards.Add(testCreditCard);
            await _dbContext.SaveChangesAsync();

            var invalidAddressId = 999;
            var result = await _controller.Checkout(creditCardId: testCreditCard.CreditCardID, addressId: invalidAddressId);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, m => m.Key == "" && m.Value.Errors.Any(e => e.ErrorMessage == "Invalid shipping address selected."));

            Assert.NotNull(viewResult.ViewData["CreditCards"]);
            Assert.NotNull(viewResult.ViewData["Addresses"]);
        }

        [Fact]
        public async Task Checkout_Fails_WhenCreditCardDoesNotBelongToUser()
        {
            var testUserId = 1;

            var otherUserId = 2;
            var otherUser = new User
            {
                UserID = otherUserId,
                Name = "Other User",
                Email = "otheruser@example.com",
                PasswordHash = "hashedpassword"
            };
            _dbContext.Users.Add(otherUser);

            var otherUserCreditCard = new CreditCard
            {
                CreditCardID = 2,
                UserID = otherUserId,
                CardNumber = "5555555555554444",
                CardHolderName = "Other User",
                ExpirationMonth = 11,
                ExpirationYear = 2024,
                CVV = "456"
            };
            _dbContext.CreditCards.Add(otherUserCreditCard);

            var testAddress = new Address
            {
                Id = 1,
                UserId = testUserId,
                FullName = "Test User",
                PhoneNumber = "123-456-7890",
                StreetAddress = "123 Test Street",
                City = "Testville",
                Province = "Test State",
                PostalCode = "12345",
                Country = "Testland"
            };
            _dbContext.Addresses.Add(testAddress);

            await _dbContext.SaveChangesAsync();

            var result = await _controller.Checkout(creditCardId: otherUserCreditCard.CreditCardID, addressId: testAddress.Id);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, m => m.Key == "" && m.Value.Errors.Any(e => e.ErrorMessage == "Invalid credit card selected."));

            Assert.NotNull(viewResult.ViewData["CreditCards"]);
            Assert.NotNull(viewResult.ViewData["Addresses"]);
        }

        [Fact]
        public async Task Checkout_Fails_WhenAddressDoesNotBelongToUser()
        {
            var testUserId = 1;

            var testCreditCard = new CreditCard
            {
                CreditCardID = 1,
                UserID = testUserId,
                CardNumber = "4111111111111111",
                CardHolderName = "Test User",
                ExpirationMonth = 12,
                ExpirationYear = 2025,
                CVV = "123"
            };
            _dbContext.CreditCards.Add(testCreditCard);

            var otherUserId = 2;
            var otherUser = new User
            {
                UserID = otherUserId,
                Name = "Other User",
                Email = "otheruser@example.com",
                PasswordHash = "hashedpassword"
            };
            _dbContext.Users.Add(otherUser);

            var otherUserAddress = new Address
            {
                Id = 2,
                UserId = otherUserId,
                FullName = "Other User",
                PhoneNumber = "987-654-3210",
                StreetAddress = "456 Other Street",
                City = "Otherville",
                Province = "Other State",
                PostalCode = "54321",
                Country = "Otherland"
            };
            _dbContext.Addresses.Add(otherUserAddress);

            await _dbContext.SaveChangesAsync();

            var result = await _controller.Checkout(creditCardId: testCreditCard.CreditCardID, addressId: otherUserAddress.Id);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, m => m.Key == "" && m.Value.Errors.Any(e => e.ErrorMessage == "Invalid shipping address selected."));

            Assert.NotNull(viewResult.ViewData["CreditCards"]);
            Assert.NotNull(viewResult.ViewData["Addresses"]);
        }
    }
}