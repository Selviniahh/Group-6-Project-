using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Group6WebProject.Data;
using Group6WebProject.Models;
using Group6WebProject.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Controllers;

public class UserController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;

    public UserController(ApplicationDbContext dbContext, IEmailService emailService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
    }

    //This can only can be invoked from main page 
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    //This can only can be invoked from main page 
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if the email is already registered. FirstOrDefaultAsync is like foreach, when an instance is found the function returns. It's async because I/O operations can block thread.  
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return View(model);
            }

            // Hash the password
            var passwordHash = HashPassword(model.Password);

            // Create the user entity
            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                PasswordHash = passwordHash,
                Status = EnrollmentStatus.ConfirmationMessageNotSent
            };

            // Add the user to the database
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Send confirmation email. 3rd argument is for ConfirmEmail method's argument value. 
            //protocol will be what being used in current Scheme which will be https or http
            //Example result: https://localhost:7015/User/ConfirmEmail?userId=4
            var callbackUrl = Url.Action("ConfirmEmail", "User",
                new { userId = user.UserID }, HttpContext.Request.Scheme);

            var emailBody = $"Hello {user.Name},<br/><br/>" +
                            "Please confirm your email by clicking the link below:<br/><br/>" +
                            $"<a href=\"{callbackUrl}\">Confirm Email</a><br/><br/>" +
                            "Thank you and have a great day.";

            await _emailService.SendEmailAsync(user.Email, "Email Confirmation", emailBody);

            // Redirect to a page informing the user to check their email
            return RedirectToAction("RegistrationConfirmation");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult RegistrationConfirmation()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found.");

        if (user.Status == EnrollmentStatus.EnrollmentConfirmed) return View("EmailAlreadyConfirmed");

        user.Status = EnrollmentStatus.EnrollmentConfirmed;
        await _dbContext.SaveChangesAsync();

        return View("EmailConfirmed");
    }

    private string HashPassword(string password)
    {
        var sha256 = SHA256.Create();
        // Convert the password string to a byte array
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

        // Convert the byte array to a string
        var builder = new StringBuilder();
        foreach (var b in bytes) builder.Append(b.ToString("x2"));

        return builder.ToString();
    }


    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Find the user by email
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            if (user.Status != EnrollmentStatus.EnrollmentConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Email not confirmed.");
                return View(model);
            }

            // Verify the password
            var passwordHash = HashPassword(model.Password);
            if (user.PasswordHash != passwordHash)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Wrong password");
                return View(model);
            }

            // Everything cheks out now Sign in the user. 
            //Claim is used for authentication about a user. 
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email)
            };

            //Use cookie authentication to sign in the user
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                // Allow refreshing the authentication session
                AllowRefresh = true,

                // Make the cookie persistent 
                IsPersistent = true,

                // Expires after 1 hour
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            //Sign in the user with all the generated claims and identities. 
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // Redirect to the home page or wherever you want
            return RedirectToAction("Index", "Home");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        //Just simply sign out from cookie authentication.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}