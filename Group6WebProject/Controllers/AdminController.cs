using Group6WebProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Group6WebProject.Data;
using System.Security.Claims;

namespace Group6WebProject.Controllers
{
  
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

        // Admin Dashboard (Only accessible by admin users)
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

        public IActionResult EditGame(int id)
        {
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

        [HttpPost]
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

        public IActionResult ApproveReview(int reviewId)
        {
            var review = _dbContext.Reviews.Find(reviewId);
            if (review == null)
            {
                return NotFound();
            }

            return View("ReviewManagement");
        }

        [HttpPost]
        public IActionResult ApproveReviewConfirmed(int reviewId)
        {
            var review = _dbContext.Reviews.Find(reviewId);
            if (review == null)
            {
                return NotFound();
            }

            review.ReviewStatus = "Approved";

            _dbContext.Reviews.Update(review);
            _dbContext.SaveChanges();

            TempData["SuccessMessage"] = "Review has been approved!";
            return RedirectToAction("ReviewManagement");
        }

        public IActionResult RejectReview(int reviewId)
        {
            var review = _dbContext.Reviews.Find(reviewId);
            if (review == null)
            {
                return NotFound();
            }

            return View("ReviewManagement");
        }

        [HttpPost]
        public IActionResult RejectReviewConfirmed(int reviewId)
        {
            var review = _dbContext.Reviews.Find(reviewId);
            if (review == null)
            {
                return NotFound();
            }

            review.ReviewStatus = "Rejected";
          

            _dbContext.Reviews.Update(review);
            _dbContext.SaveChanges();

            TempData["ErrorMessage"] = "Review has been rejected!";
            return RedirectToAction("ReviewManagement");
        }

        public IActionResult ReviewManagement()
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var pendingReviews = _dbContext.Reviews
                .Where(r => r.ReviewStatus == "Pending")
                .Include(r => r.Game)
                .Include(r => r.User)
                .ToList();

            return View(pendingReviews);
        }
    }
}
