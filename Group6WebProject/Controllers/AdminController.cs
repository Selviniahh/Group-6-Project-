using Group6WebProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Group6WebProject.Data;
using System.Security.Claims;

namespace Group6WebProject.Controllers
{
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

            var games = _dbContext.Games.ToList();
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

        // Edit Game
        public IActionResult EditGame(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var game = _dbContext.Games.Find(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        [HttpPost]
        public IActionResult EditGame(Game game)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                _dbContext.Games.Update(game);
                _dbContext.SaveChanges();
                return RedirectToAction("GameManagement");
            }
            return View(game);
        }

        // Delete Game
        public IActionResult DeleteGame(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var game = _dbContext.Games.Find(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        [HttpPost,]
        public IActionResult DeleteGameConfirmed(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var game = _dbContext.Games.Find(id);
            if (game != null)
            {
                _dbContext.Games.Remove(game);
                _dbContext.SaveChanges(); // Persist changes to the database
            }
            return RedirectToAction("GameManagement"); // Redirect back to the Game Management page
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
        // GET: Display Edit Event Form
public IActionResult EditEvent(int id)
{
    var eventItem = _dbContext.Events.Find(id);
    if (eventItem == null)
    {
        return NotFound();
    }

    return View(eventItem);
}

// POST: Edit Event
[HttpPost]
public IActionResult EditEvent(Event eventItem)
{
    if (ModelState.IsValid)
    {
        _dbContext.Events.Update(eventItem);
        _dbContext.SaveChanges();
        
        return RedirectToAction("EventManagement");
    }

    return View(eventItem); // Return to the view if validation fails
}
// GET: Display Delete Event Confirmation
public IActionResult DeleteEvent(int id)
{
    var eventItem = _dbContext.Events.Find(id);
    if (eventItem == null)
    {
        return NotFound();
    }

    return View(eventItem);
}

// POST: Delete Event Confirmed
[HttpPost]
public IActionResult DeleteEventConfirmed(int id)
{
    var eventItem = _dbContext.Events.Find(id);
    if (eventItem != null)
    {
        _dbContext.Events.Remove(eventItem);
        _dbContext.SaveChanges();
      
    }

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
}
