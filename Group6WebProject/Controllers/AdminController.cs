using Group6WebProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Group6WebProject.Data;
using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SelectPdf;

namespace Group6WebProject.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public AdminController(ApplicationDbContext context, ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
        {
            _dbContext = context;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }


        private async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var routeData = new RouteData();
            routeData.Values["controller"] = "Admin";
            var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: false);

                if (!viewResult.Success)
                {
                    throw new FileNotFoundException($"View {viewName} not found.");
                }

                var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                };

                var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    tempData,
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);

                return sw.ToString();
            }
        }

        public async Task<IActionResult> GenerateGameListReport(string format)
        {
            var games = await _dbContext.Games.ToListAsync();

            if (format == "pdf")
            {
                var htmlContent = await RenderViewToStringAsync("GameListReport", games);

                HtmlToPdf converter = new HtmlToPdf();
                PdfDocument doc = converter.ConvertHtmlString(htmlContent);

                var pdfBytes = doc.Save();
                doc.Close();

                return File(pdfBytes, "application/pdf", "GameListReport.pdf");
            }
            else if (format == "excel")
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Games");

                    worksheet.Cell(1, 1).Value = "Title";
                    worksheet.Cell(1, 2).Value = "Description";
                    worksheet.Cell(1, 3).Value = "Genre";
                    worksheet.Cell(1, 4).Value = "Price";
                    worksheet.Cell(1, 5).Value = "Platform";
                    worksheet.Cell(1, 6).Value = "Release Date";

                    int row = 2;
                    foreach (var game in games)
                    {
                        worksheet.Cell(row, 1).Value = game.Title;
                        worksheet.Cell(row, 2).Value = game.Description;
                        worksheet.Cell(row, 3).Value = game.Genre;
                        worksheet.Cell(row, 4).Value = game.Price;
                        worksheet.Cell(row, 5).Value = game.Platform;
                        worksheet.Cell(row, 6).Value = game.ReleaseDate.ToString("yyyy-MM-dd");
                        row++;
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GameListReport.xlsx");
                    }
                }
            }

            return RedirectToAction("Reports");
        }
        
        // Generate Member List Report
        public async Task<IActionResult> GenerateMemberListReport(int UserID, string format)
        {
            var members = await _dbContext.Users
                .Select(u => new
                {
                    u.UserID,
                    u.Name,
                    u.Email,
                    u.IsAdmin
                })
                .ToListAsync();

            if (format == "pdf")
            {
                var htmlContent = await RenderViewToStringAsync("MemberListReport", members);

                HtmlToPdf converter = new HtmlToPdf();
                PdfDocument doc = converter.ConvertHtmlString(htmlContent);

                var pdfBytes = doc.Save();
                doc.Close();

                return File(pdfBytes, "application/pdf", "MemberListReport.pdf");
            }
            else if (format == "excel")
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Members");

                    worksheet.Cell(1, 1).Value = "Member ID";
                    worksheet.Cell(1, 2).Value = "Display Name";
                    worksheet.Cell(1, 3).Value = "Email";
                    worksheet.Cell(1, 5).Value = "Is Admin";

                    int row = 2;
                    foreach (var member in members)
                    {
                        worksheet.Cell(row, 1).Value = member.UserID;
                        worksheet.Cell(row, 2).Value = member.Name;
                        worksheet.Cell(row, 3).Value = member.Email;
                        worksheet.Cell(row, 5).Value = member.IsAdmin;
                        row++;
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MemberListReport.xlsx");
                    }
                }
            }

            return RedirectToAction("Reports");
        }

// Generate Member Detail Report
public async Task<IActionResult> GenerateMemberDetailReport(int userId, string format)
{
    var member = await _dbContext.Users
        .Include(u => u.Reviews) // Assuming the member has reviews
        .Include(u => u.Events) // Assuming the member has events they attended
        .FirstOrDefaultAsync(u => u.UserID == userId);

    if (member == null)
    {
        return NotFound();
    }

    if (format == "pdf")
    {
        var htmlContent = await RenderViewToStringAsync("MemberDetailReport", member);

        // Convert HTML to PDF
        HtmlToPdf converter = new HtmlToPdf();
        PdfDocument doc = converter.ConvertHtmlString(htmlContent);

        var pdfBytes = doc.Save();
        doc.Close();

        var fileName = $"MemberDetailReport_{member.Name}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
    else if (format == "excel")
    {
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Member Detail");

            worksheet.Cell(1, 1).Value = "Member ID";
            worksheet.Cell(1, 2).Value = member.UserID;

            worksheet.Cell(2, 1).Value = "Display Name";
            worksheet.Cell(2, 2).Value = member.Name;

            worksheet.Cell(3, 1).Value = "Email";
            worksheet.Cell(3, 2).Value = member.Email;
            
            worksheet.Cell(5, 1).Value = "Is Admin";
            worksheet.Cell(5, 2).Value = member.IsAdmin;

            if (member.Reviews != null && member.Reviews.Any())
            {
                var reviewSheet = workbook.Worksheets.Add("Reviews");

                // Add review headers
                reviewSheet.Cell(1, 1).Value = "Game Title";
                reviewSheet.Cell(1, 2).Value = "Review Text";
                reviewSheet.Cell(1, 4).Value = "Date";

                int row = 2;
                foreach (var review in member.Reviews)
                {
                    reviewSheet.Cell(row, 1).Value = review.Game.Title;
                    reviewSheet.Cell(row, 2).Value = review.ReviewText;
                    reviewSheet.Cell(row, 4).Value = review.SubmissionDate.ToString("yyyy-MM-dd");
                    row++;
                }
            }

            if (member.Events != null && member.Events.Any())
            {
                var eventSheet = workbook.Worksheets.Add("Events");

                // Add event headers
                eventSheet.Cell(1, 1).Value = "Event Title";
                eventSheet.Cell(1, 2).Value = "Event Date";
                eventSheet.Cell(1, 3).Value = "Location";

                int row = 2;
                foreach (var eventItem in member.Events)
                {
                    eventSheet.Cell(row, 1).Value = eventItem.Name;
                    eventSheet.Cell(row, 2).Value = eventItem.EventDate.ToString("yyyy-MM-dd");
                    eventSheet.Cell(row, 3).Value = eventItem.Description;
                    row++;
                }
            }

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                var fileName = $"MemberDetailReport_{member.Name}.xlsx";
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }

    return RedirectToAction("Reports");
}

        // Generate Game Detail Report
        public async Task<IActionResult> GenerateGameDetailReport(int gameId, string format)
        {
            var game = await _dbContext.Games
                .Include(g => g.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
            {
                return NotFound();
            }

            if (format == "pdf")
            {
                var htmlContent = await RenderViewToStringAsync("GameDetailReport", game);

                // Generate the perfect looking PDF 
                HtmlToPdf converter = new HtmlToPdf();
                PdfDocument doc = converter.ConvertHtmlString(htmlContent);

                var pdfBytes = doc.Save();
                doc.Close();

                var fileName = $"GameDetailReport_{game.Title}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            else if (format == "excel")
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Game Detail");

                    worksheet.Cell(1, 1).Value = "Title";
                    worksheet.Cell(1, 2).Value = game.Title;

                    worksheet.Cell(2, 1).Value = "Description";
                    worksheet.Cell(2, 2).Value = game.Description;

                    worksheet.Cell(3, 1).Value = "Genre";
                    worksheet.Cell(3, 2).Value = game.Genre;

                    worksheet.Cell(4, 1).Value = "Price";
                    worksheet.Cell(4, 2).Value = game.Price;

                    worksheet.Cell(5, 1).Value = "Platform";
                    worksheet.Cell(5, 2).Value = game.Platform;

                    worksheet.Cell(6, 1).Value = "Release Date";
                    worksheet.Cell(6, 2).Value = game.ReleaseDate.ToString("yyyy-MM-dd");

                    if (game.Reviews != null && game.Reviews.Any())
                    {
                        var reviewSheet = workbook.Worksheets.Add("Reviews");

                        // Add headers
                        reviewSheet.Cell(1, 1).Value = "User";
                        reviewSheet.Cell(1, 2).Value = "Date";
                        reviewSheet.Cell(1, 3).Value = "Review Text";

                        int row = 2;
                        foreach (var review in game.Reviews)
                        {
                            reviewSheet.Cell(row, 1).Value = review.User.Name;
                            reviewSheet.Cell(row, 2).Value = review.SubmissionDate.ToString("yyyy-MM-dd");
                            reviewSheet.Cell(row, 3).Value = review.ReviewText;
                            row++;
                        }
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();

                        var fileName = $"GameDetailReport_{game.Title}.xlsx";
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }

            return RedirectToAction("Reports");
        }
        

        public IActionResult Reports()
        {
            var users = _dbContext.Users.ToList();  // Assuming _dbContext is your database context
            var games = _dbContext.Games.ToList();
            ViewBag.Users = users;  // Pass the list of users to the view
            ViewBag.Games = games; // Similarly, pass the games to the view
            return View();
        }
        
        public bool IsAdmin()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var user = _dbContext.Users.Find(userId);
                return user != null && user.IsAdmin;
            }

            return false;
        }

        // Admin Dashboard
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
            ViewBag.Genres = GetGenres();
            ViewBag.Platforms = GetPlatforms();
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
            
            if (ModelState.IsValid)
            {
                _dbContext.Games.Add(game);
                _dbContext.SaveChanges();
                return RedirectToAction("GameManagement");
            }

            ViewBag.Genres = GetGenres();
            ViewBag.Platforms = GetPlatforms();
            return View("GameManagement", _dbContext.Games.ToList());
        }

        public IActionResult EditGame(int id)
        {
            var game = _dbContext.Games.Find(id);
            if (game == null)
            {
                return NotFound();
            }

            ViewBag.Genres = GetGenres();
            ViewBag.Platforms = GetPlatforms();
            return View(game);
        }


// POST: Admin/EditGame
        [HttpPost]
        public IActionResult EditGame(Game game)
        {
            ModelState.Remove("Reviews");
            ModelState.Remove("Ratings");

            if (ModelState.IsValid)
            {
                _dbContext.Games.Update(game);
                _dbContext.SaveChanges();
                return RedirectToAction("GameManagement");
            }

            return View(game); // Return the view with the model if validation fails
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

        private List<string> GetGenres()
        {
            return new List<string>
            {
                "Action",
                "Adventure",
                "Role-Playing",
                "Simulation",
                "Strategy",
                "Sports",
                "Puzzle",
                "Shooter",
                "Horror",
                "Other"
            };
        }

        private List<string> GetPlatforms()
        {
            return new List<string>
            {
                "Windows",
                "MacOS",
                "Linux",
                "PlayStation",
                "Xbox",
                "Nintendo Switch",
                "Mobile",
                "Web",
                "Other"
            };
        }
    }
}