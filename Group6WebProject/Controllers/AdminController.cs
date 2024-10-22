using Group6WebProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Group6WebProject.Data;
using System.Security.Claims;

namespace Group6WebProject.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public AdminController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private bool IsAdmin()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            var user = _dbContext.Users.Find(userId);
            return user != null && user.IsAdmin;
        }
        return false;
    }

    public IActionResult Index()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return View();
    }

    // Game Management
    public IActionResult GameManagement()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var games = _dbContext.Set<Game>().ToList();
        return View(games);
    }

    [HttpPost]
    public IActionResult AddGame(Game game)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        _dbContext.Games.Add(game);
        _dbContext.SaveChanges();
        return RedirectToAction("GameManagement");
    }

    // Event Management
    public IActionResult EventManagement()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var events = _dbContext.Set<Event>().ToList();
        return View(events);
    }

    [HttpPost]
    public IActionResult AddEvent(Event eventItem)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        _dbContext.Events.Add(eventItem);
        _dbContext.SaveChanges();
        return RedirectToAction("EventManagement");
    }

    // Game Reviews
    public IActionResult GameReviews()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var reviews = _dbContext.Set<Review>().ToList();
        return View(reviews);
    }

    [HttpPost]
    public IActionResult AddReview(Review review)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        _dbContext.Reviews.Add(review);
        _dbContext.SaveChanges();
        return RedirectToAction("GameReviews");
    }
}
