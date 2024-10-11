using Group6WebProject.Models;
using Microsoft.AspNetCore.Mvc;

namespace Group6WebProject.Controllers;

public class AdminController : Controller
{
    private static List<Game> games = new List<Game>();
    private static List<Event> events = new List<Event>();
    private static List<Review> reviews = new List<Review>();

    public IActionResult Index()
    {
        return View();
    }

    // Game Management
    public IActionResult GameManagement()
    {
        return View(games);
    }

    [HttpPost]
    public IActionResult AddGame(Game game)
    {
        games.Add(game);
        return RedirectToAction("GameManagement");
    }

    // Event Management
    public IActionResult EventManagement()
    {
        return View(events);
    }

    [HttpPost]
    public IActionResult AddEvent(Event eventItem)
    {
        events.Add(eventItem);
        return RedirectToAction("EventManagement");
    }

    // Game Reviews
    public IActionResult GameReviews()
    {
        return View(reviews);
    }

    [HttpPost]
    public IActionResult AddReview(Review review)
    {
        reviews.Add(review);
        return RedirectToAction("GameReviews");
    }

   
}