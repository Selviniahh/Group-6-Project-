using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Data;
using Group6WebProject.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Controllers;

[Authorize]
public class CreditCardController : Controller
{
    private readonly ApplicationDbContext _context;

    public CreditCardController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /CreditCard/
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var creditCards = await _context.CreditCards
            .Where(cc => cc.UserID == userId)
            .ToListAsync();
        return View(creditCards);
    }

    // GET: /CreditCard/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /CreditCard/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreditCard creditCard)
    {
        if (ModelState.IsValid)
        {
            creditCard.UserID = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            _context.CreditCards.Add(creditCard);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // If ModelState is invalid, collect the errors
        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
        ViewBag.ValidationErrors = errors; // Pass errors to the view

        return View(creditCard);
    }

    // GET: /CreditCard/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var creditCard = await _context.CreditCards.FirstOrDefaultAsync(cc => cc.CreditCardID == id && cc.UserID == userId);
        if (creditCard == null)
        {
            return NotFound();
        }

        return View(creditCard);
    }

// POST: /CreditCard/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreditCard creditCard)
    {
        if (id != creditCard.CreditCardID)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            try
            {
                creditCard.UserID = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _context.Update(creditCard);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CreditCardExists(creditCard.CreditCardID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        return View(creditCard);
    }

    private bool CreditCardExists(int id)
    {
        return _context.CreditCards.Any(e => e.CreditCardID == id);
    }

// GET: /CreditCard/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var creditCard = await _context.CreditCards.FirstOrDefaultAsync(cc => cc.CreditCardID == id && cc.UserID == userId);
        if (creditCard == null)
        {
            return NotFound();
        }

        return View(creditCard);
    }

// POST: /CreditCard/DeleteConfirmed/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var creditCard = await _context.CreditCards.FirstOrDefaultAsync(cc => cc.CreditCardID == id && cc.UserID == userId);
        if (creditCard != null)
        {
            _context.CreditCards.Remove(creditCard);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}