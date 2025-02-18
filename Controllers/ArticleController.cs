// Jack Gerber - A01266976
// Date: Feb 16, 2025

using BlogApp.Models;
using BlogApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Controllers;

[Authorize] // Ensures only authenticated users can access this controller
public class ArticleController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ArticleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET action for creating a new article
    [Authorize(Roles = "Contributor,Admin")]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(); // Return unauthorized if user not found
        }

        // Pre-populate a new Article model with the logged-in user's username
        var article = new Article
        {
            ContributorUsername = user.UserName,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };

        return View(article); // Return the view with the new article model
    }

    // POST action for creating a new article
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Contributor,Admin")]
    public async Task<IActionResult> Create(Article article)
    {
        ModelState.Remove(nameof(article.ContributorUsername)); // Remove ContributorUsername error

        if (!ModelState.IsValid)
        {
            return View(article); // Return view with errors if model is invalid
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.UserName))
        {
            ModelState.AddModelError("", "User information is not available. Please log in again.");
            return View(article); // Return view with error if user is not found
        }

        article.ContributorUsername = user.UserName; // Set ContributorUsername to the logged-in user's username
        article.CreateDate = DateTime.UtcNow;

        // Set default start and end dates if not provided
        article.StartDate = article.StartDate == default ? DateTime.UtcNow : article.StartDate;
        article.EndDate = article.EndDate == default ? DateTime.UtcNow.AddMonths(1) : article.EndDate;

        _context.Articles.Add(article);
        await _context.SaveChangesAsync(); // Save the article to the database

        return RedirectToAction("Index", "Home"); // Redirect to the home page after saving
    }

    // GET action to edit an existing article
    [Authorize(Roles = "Contributor,Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound(); // Return NotFound if id is null

        var article = await _context.Articles.FindAsync(id);
        if (article == null) return NotFound(); // Return NotFound if article not found

        if (!User.IsInRole("Admin") && article.ContributorUsername != User.Identity.Name)
        {
            return Forbid(); // Contributors can only edit their own articles
        }

        return View(article); // Return the edit view with the article
    }

    // POST action to update an existing article
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Contributor,Admin")]
    public async Task<IActionResult> Edit(int id, Article article)
    {
        if (id != article.ArticleId) return NotFound(); // Return NotFound if article ID doesn't match

        var existingArticle = await _context.Articles.FindAsync(id);
        if (existingArticle == null) return NotFound(); // Return NotFound if article doesn't exist

        if (!User.IsInRole("Admin") && existingArticle.ContributorUsername != User.Identity.Name)
        {
            return Forbid(); // Restrict editing to the owner or an admin
        }

        // Update the article with the new values
        existingArticle.Title = article.Title;
        existingArticle.Body = article.Body;
        existingArticle.StartDate = article.StartDate;
        existingArticle.EndDate = article.EndDate;

        try
        {
            _context.Update(existingArticle);
            await _context.SaveChangesAsync(); // Save updated article to the database
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ArticleExists(article.ArticleId)) return NotFound(); // Check if article still exists
            else throw; // Rethrow exception if the article exists but failed to update
        }

        return RedirectToAction("Index", "Home"); // Redirect to the home page after saving
    }

    // POST action to delete an article (confirm deletion)
    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Contributor,Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null) return NotFound(); // Return NotFound if article doesn't exist

        if (!User.IsInRole("Admin") && article.ContributorUsername != User.Identity.Name)
        {
            return Forbid(); // Restrict deletion to the owner or an admin
        }

        _context.Articles.Remove(article);
        await _context.SaveChangesAsync(); // Delete the article from the database

        return RedirectToAction("Index", "Home"); // Redirect to the home page after deletion
    }

    // GET action to view all articles
    public async Task<IActionResult> Index()
    {
        var articles = await _context.Articles
            .Where(a => a.StartDate <= DateTime.UtcNow && a.EndDate >= DateTime.UtcNow)
            .ToListAsync(); // Get list of articles that are currently active

        var user = await _userManager.GetUserAsync(User); // Get the current logged-in user

        // Attach author names to articles
        foreach (var article in articles)
        {
            var author = await _userManager.FindByNameAsync(article.ContributorUsername);
            article.ContributorUsername = author != null ? $"{author.FirstName} {author.LastName}" : "Unknown Author";
        }

        return View(Tuple.Create(user, articles)); // Return the view with the articles and user data
    }

    // GET action to view the details of a specific article
    public async Task<IActionResult> Details(int id)
    {
        var article = await _context.Articles.FirstOrDefaultAsync(m => m.ArticleId == id);
        if (article == null) return NotFound(); // Return NotFound if article doesn't exist

        return View(article); // Return the details view with the article data
    }

    // Helper method to check if an article exists by ID
    private bool ArticleExists(int id)
    {
        return _context.Articles.Any(e => e.ArticleId == id);
    }
}
