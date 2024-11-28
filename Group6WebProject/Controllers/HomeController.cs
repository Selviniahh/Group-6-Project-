using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Data;
using System.Linq;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public HomeController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IActionResult Index()
    {
        var games = _dbContext.Games.ToList();
        return View(games);
    }
}