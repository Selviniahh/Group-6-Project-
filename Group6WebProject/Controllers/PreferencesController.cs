using Group6WebProject.Models;
using Microsoft.AspNetCore.Mvc;

namespace Group6WebProject.Controllers;

public class PreferencesController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult SavePreferences(MemberPreferences preferences)
    {
        if (ModelState.IsValid) return RedirectToAction("Success");
        return View("Index", preferences);
    }

    public IActionResult Success()
    {
        return View();
    }
}