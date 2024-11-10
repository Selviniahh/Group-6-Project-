using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Group6WebProject.Data;
using Group6WebProject.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
namespace Group6WebProject.Tests
{
    public class UserControllerTests : IDisposable
    {
        private readonly UserController _controller;
        private readonly ApplicationDbContext _context;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<SignInManager<User>> _signInManagerMock;
        private readonly IReCaptchaService _reCaptchaService;
        private readonly Mock<IReCaptchaService> _reCaptchaServiceMock;

        public UserControllerTests()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Make Unique DB per test
                .Options;

            _context = new ApplicationDbContext(options); 

            // Mock services
            _emailServiceMock = new Mock<IEmailService>();

            // Mock UserManager
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            // Mock SignInManager
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            _signInManagerMock = new Mock<SignInManager<User>>(
                _userManagerMock.Object,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                null, null, null, null
            );

            // Initialize the controller with the mocked services
            _controller = new UserController(_context, _emailServiceMock.Object, _reCaptchaService)
            {
                Url = GetMockUrlHelper("https://localhost/User/ConfirmEmail?userId=0"),
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Set HttpContext.Request.Scheme just like in the actual environment
            _controller.ControllerContext.HttpContext.Request.Scheme = "https";
            
            // Mock IReCaptchaService to always return true (bypass reCAPTCHA)
            _reCaptchaServiceMock = new Mock<IReCaptchaService>();
            _reCaptchaServiceMock.Setup(r => r.VerifyToken(It.IsAny<string>())).ReturnsAsync(true);
        }

        // Create a mock IUrlHelper
        private static IUrlHelper GetMockUrlHelper(string returnValue)
        {
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(o => o.Action(It.IsAny<UrlActionContext>())).Returns(returnValue);
            return urlHelperMock.Object;
        }

        // Dispose method for cleaning up after each test
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task UnConfirmedEmailShouldReturnError()
        {
            // Arrange
            var password = "Password123!";
            var user = new User
            {
                UserID = 1,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = UserController.HashPassword(password),
                Status = EnrollmentStatus.ConfirmationMessageNotSent
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel
            {
                Email = user.Email,
                Password = password
            };

            // Act
            await _controller.Login(model);

            // Assert
            Assert.False(_controller.ModelState.IsValid);
            var error = _controller.ModelState[string.Empty].Errors.First();
            Assert.Equal("Email not confirmed.", error.ErrorMessage);
        }

        


        [Fact]
        public async Task ProfileReturnsProfileModel()
        {
            // Arrange
            int userId = 1;
            var user = new User
            {
                UserID = userId,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = UserController.HashPassword("Password123!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            //Simulate httpcontext authentication 
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = await _controller.Profile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Profile>(viewResult.Model);
        }
        
        [Fact]
        public async Task SaveProfileAppliesChanges()
        {
            // **Arrange**
            var user = new User
            {
                UserID = 2,
                Name = "Another User",
                Email = "another@example.com",
                PasswordHash = UserController.HashPassword("AnotherPassword!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Simulate authenticated HttpContext
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Set up TempData again if ControllerContext is re-assigned
            var tempDataMock = new Mock<ITempDataProvider>();
            _controller.TempData = new TempDataDictionary(_controller.ControllerContext.HttpContext, tempDataMock.Object);

            // Create and save initial profile
            var initialProfile = new Profile
            {
                UserId = user.UserID,
                Name = user.Name,
                // Biography = "Initial Biography",
                // FavouriteVideoGame = "Initial Game",
                // DateOfBirth = new DateTime(1990, 1, 1),
                // Gender = Gender.Male,
                // Country = "Initial Country",
                // ContactNumber = "0000000000",
                // ReceivePromotionalEmails = false,
                // LastLogin = DateTime.Now
            };
            _context.Profiles.Add(initialProfile);
            await _context.SaveChangesAsync();

            // Modify the profile
            var updatedProfile = new Profile
            {
                UserId = user.UserID,
                Name = user.Name,
                // Biography = "Updated Biography",
                // FavouriteVideoGame = "Updated Game",
                // DateOfBirth = new DateTime(1990, 1, 1),
                // Gender = Gender.Female,
                // Country = "Updated Country",
                // ContactNumber = "1111111111",
                // ReceivePromotionalEmails = true
            };

            // **Act**
            var result = await _controller.SaveProfile(updatedProfile);

            // **Assert**
            // Assert.Equal("Updated Biography", savedProfile.Biography);
            // Assert.Equal("Updated Game", savedProfile.FavouriteVideoGame);
            // Assert.Equal(Gender.Female, savedProfile.Gender);
            // Assert.Equal("Updated Country", savedProfile.Country);
            // Assert.Equal("1111111111", savedProfile.ContactNumber);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);
        }
        
        [Fact]
        public async Task ProfileFetchedUserContentCorrectly()
        {
            // Arrange
            int userId = 1;
            var user = new User
            {
                UserID = userId,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = UserController.HashPassword("Password123!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var profile = new Profile
            {
                Id = 1,
                UserId = userId,
                Name = "Test User",
                Gender = Gender.PreferNotToSay,
                BirthDate = new System.DateTime(1990, 1, 1),
                ReceiveCvgs = true
            };
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Simulate httpcontext authentication 
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await _controller.Profile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Profile>(viewResult.Model);
            Assert.Equal(profile.Name, model.Name);
            Assert.Equal(userId, model.UserId);
            Assert.Equal(profile.Gender, model.Gender);
            Assert.Equal(profile.BirthDate, model.BirthDate);
            Assert.Equal(profile.ReceiveCvgs, model.ReceiveCvgs);
        }
        
        [Fact]
        public async Task Login_InvalidEmail_ShouldReturnError()
        {
            // Arrange
            var loginModel = new LoginViewModel
            {
                Email = "nonexistent@example.com",
                Password = "AnyPassword123!"
            };

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, m => m.Key == "" && m.Value.Errors.Any(e => e.ErrorMessage == "Invalid login attempt. Email does not exist"));
        }
        
        [Fact]
        public async Task Login_InvalidPassword_ShouldReturnError()
        {
            // Arrange
            var testUser = new User
            {
                UserID = 1,
                Name = "Test User",
                Email = "testuser@example.com",
                PasswordHash = UserController.HashPassword("Password123!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            var loginModel = new LoginViewModel
            {
                Email = "testuser@example.com",
                Password = "WrongPassword!"
            };

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, m => m.Key == "" && m.Value.Errors.Any(e => e.ErrorMessage == "Invalid login attempt. You have 2 more attempt(s) before your account is locked."));
        }
        

        [Fact]
        public async Task ForgetPassword_EmailDoesNotExist_ShouldReturnError()
        {
            // Arrange
            var forgetPasswordModel = new ForgetPasswordViewModel
            {
                Email = "nonexistent@example.com"
            };

            // Act
            var result = await _controller.ForgetPassword(forgetPasswordModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, m => m.Key == "" && m.Value.Errors.Any(e => e.ErrorMessage == "Email does not exist."));

            // Verify that SendEmailAsync was not called
            _emailServiceMock.Verify(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

       
        [Fact]
        public async Task ConfirmEmail_Success()
        {
            // Arrange
            var testUserId = 1;
            var testUser = new User
            {
                UserID = testUserId,
                Name = "Test User",
                Email = "testuser@example.com",
                PasswordHash = UserController.HashPassword("Password123!"),
                Status = EnrollmentStatus.ConfirmationMessageNotSent
            };
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ConfirmEmail(testUserId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("EmailConfirmed", viewResult.ViewName);

            // Verify that user's status is updated
            var updatedUser = await _context.Users.FindAsync(testUserId);
            Assert.Equal(EnrollmentStatus.EnrollmentConfirmed, updatedUser.Status);
        }

        
        [Fact]
        public async Task AddFriend_Success()
        {
            // Arrange
            var user1 = new User
            {
                UserID = 1,
                Name = "User One",
                Email = "user1@example.com",
                PasswordHash = UserController.HashPassword("Password123!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            var user2 = new User
            {
                UserID = 2,
                Name = "User Two",
                Email = "user2@example.com",
                PasswordHash = UserController.HashPassword("Password456!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            // Authenticate as user1
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user1.UserID.ToString()),
                new Claim(ClaimTypes.Name, user1.Name),
                new Claim(ClaimTypes.Email, user1.Email)
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await _controller.AddFriend(userId: user1.UserID, friendId: user2.UserID);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("FriendsAndFamilyDetails", redirectResult.ActionName);
            Assert.Equal(user1.UserID, redirectResult.RouteValues["userId"]);

            // Verify that user2 is in user1's FriendsAndFamily
            var updatedUser1 = await _context.Users
                .Include(u => u.FriendsAndFamily)
                .FirstOrDefaultAsync(u => u.UserID == user1.UserID);
            Assert.Contains(updatedUser1.FriendsAndFamily, f => f.UserID == user2.UserID);
        }

        
        [Fact]
        public async Task ConfirmEmail_EmailAlreadyConfirmed_ShouldShowEmailAlreadyConfirmedView()
        {
            // Arrange
            var testUserId = 1;
            var testUser = new User
            {
                UserID = testUserId,
                Name = "Test User",
                Email = "testuser@example.com",
                PasswordHash = UserController.HashPassword("Password123!"),
                Status = EnrollmentStatus.EnrollmentConfirmed // Already confirmed
            };
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ConfirmEmail(testUserId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("EmailAlreadyConfirmed", viewResult.ViewName);
        }

        
        [Fact]
        public async Task AddFriend_AddingExistingFriend_ShouldNotDuplicate()
        {
            // Arrange
            var user1 = new User
            {
                UserID = 1,
                Name = "User One",
                Email = "user1@example.com",
                PasswordHash = UserController.HashPassword("Password123!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            var user2 = new User
            {
                UserID = 2,
                Name = "User Two",
                Email = "user2@example.com",
                PasswordHash = UserController.HashPassword("Password456!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            // Authenticate as user1 and add user2 as friend
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user1.UserID.ToString()),
                new Claim(ClaimTypes.Name, user1.Name),
                new Claim(ClaimTypes.Email, user1.Email)
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // First addition
            var firstResult = await _controller.AddFriend(userId: user1.UserID, friendId: user2.UserID);
            var firstRedirect = Assert.IsType<RedirectToActionResult>(firstResult);
            Assert.Equal("FriendsAndFamilyDetails", firstRedirect.ActionName);
            Assert.Equal(user1.UserID, firstRedirect.RouteValues["userId"]);

            // Second addition attempt
            var secondResult = await _controller.AddFriend(userId: user1.UserID, friendId: user2.UserID);
            var secondRedirect = Assert.IsType<RedirectToActionResult>(secondResult);
            Assert.Equal("FriendsAndFamilyDetails", secondRedirect.ActionName);
            Assert.Equal(user1.UserID, secondRedirect.RouteValues["userId"]);

            // Verify that user2 is only added once
            var updatedUser1 = await _context.Users
                .Include(u => u.FriendsAndFamily)
                .FirstOrDefaultAsync(u => u.UserID == user1.UserID);
            Assert.Single(updatedUser1.FriendsAndFamily.Where(f => f.UserID == user2.UserID));
        }
    }
}