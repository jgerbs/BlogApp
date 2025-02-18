// Jack Gerber - A01266976
// Date: Feb 16, 2025

using BlogApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BlogApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    // GET action to show the registration form
    public IActionResult Register()
    {
        var model = new RegisterViewModel();  // Initialize model for registration form
        return View(model);  // Return the registration view with the model
    }

    // POST action to handle form submission for user registration
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Create a new user from the provided registration data
            var user = new ApplicationUser
            {
                UserName = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                IsApproved = false  // Account needs approval before access
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Ensure the "Contributor" role exists
                if (!await _roleManager.RoleExistsAsync("Contributor"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Contributor"));
                }

                // Add the new user to the Contributor role
                await _userManager.AddToRoleAsync(user, "Contributor");

                // Redirect to the Approval Pending page
                return RedirectToAction("ApprovalPending", "Account");
            }

            // Add any errors from the result to the model state
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        return View(model);  // Return the registration view with the model (if invalid)
    }

    // Action to show the "Approval Pending" view
    public IActionResult ApprovalPending()
    {
        return View();  // Show the page informing the user that approval is pending
    }

    // GET action to show the login form
    public IActionResult Login()
    {
        var model = new LoginViewModel();  // Initialize model for login form
        return View(model);  // Return the login view with the model
    }

    // POST action to handle form submission for user login
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Fetch the user by username (email)
            var user = await _userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            // Check if the user has been approved
            if (!user.IsApproved)
            {
                // Redirect to the Approval Pending page if the user is not approved
                return RedirectToAction("ApprovalPending", "Account");
            }

            // Sign the user in if valid credentials are provided
            var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);  // Refresh the sign-in
                return RedirectToAction("Index", "Home");  // Redirect to the home page
            }

            ModelState.AddModelError("", "Invalid login attempt.");
        }
        return View(model);  // Return the login view with the model (if invalid)
    }

    // Action to show a list of unapproved users (only accessible by Admin)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveUsers()
    {
        var users = await _userManager.Users
                                    .Where(u => !u.IsApproved) // Filter to fetch only unapproved users
                                    .ToListAsync();

        return View(users);  // Pass the list of unapproved users to the view
    }

    // Action to approve a user (only accessible by Admin)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);  // Fetch user by ID

        if (user != null && !user.IsApproved)
        {
            user.IsApproved = true;  // Mark user as approved
            var result = await _userManager.UpdateAsync(user);  // Update the user record

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User approved successfully!";  // Success message
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to approve user.";  // Error message if approval failed
            }
        }

        return RedirectToAction("ApproveUsers");  // Redirect back to the unapproved users list page
    }

    // Action to log the user out
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();  // Sign the user out
        return RedirectToAction("Index", "Home");  // Redirect to the home page
    }
}

