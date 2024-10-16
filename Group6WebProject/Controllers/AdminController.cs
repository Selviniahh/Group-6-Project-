﻿using Group6WebProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Group6WebProject.Controllers;
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private static List<Game> games = new();
    private static List<Event> events = new();
    private static List<Review> reviews = new();


    public IActionResult Index()
    {
        // Log claims for debugging
        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"{claim.Type}: {claim.Value}");
        }

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