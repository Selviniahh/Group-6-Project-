using System.Security.Claims;
using Group6WebProject.Data;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Models;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Controllers;

public class EventsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public EventsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult AllEvents()
    {
        var events = _dbContext.Events.ToList();
        return View(events);
    }

    public IActionResult EventDetails(int id)
    {
        var eventItem = _dbContext.Events
            .Include(e => e.EventRegister)
            .ThenInclude(er => er.User)
            .FirstOrDefault(e => e.Id == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        return View(eventItem);
    }

    [HttpPost]
    public IActionResult RegisterForEvent(int eventId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }
        // Check if the user is already registered for the event
        var existingRegistration = _dbContext.EventRegister
            .FirstOrDefault(r => r.UserId == userId && r.EventId == eventId);

        if (existingRegistration != null)
        {
            TempData["InfoMessage"] = "You are already registered for this event!";
        }
        else
        {
            var registration = new EventRegister
            {
                UserId = userId,
                EventId = eventId
            };

            _dbContext.EventRegister.Add(registration);
            _dbContext.SaveChanges();

            TempData["SuccessMessage"] = "You have registered for this event, see you there!";
        }

        // Reload the view
        var eventItem = _dbContext.Events
            .Include(e => e.EventRegister)
            .ThenInclude(er => er.User)
            .FirstOrDefault(e => e.Id == eventId);

        return View("EventDetails", eventItem);

    }

}