// Jack Gerber - A01266976
// Date: Feb 16, 2025

using BlogApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BlogApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add SQLite Database and Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add Controllers and Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();  // Add RoleManager for role management
    var signInManager = services.GetRequiredService<SignInManager<ApplicationUser>>();  // Inject SignInManager

    // Apply migrations first
    await context.Database.MigrateAsync();

    // Create "Admin" role if it doesn't exist
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Create "Contributor" role if it doesn't exist
    if (!await roleManager.RoleExistsAsync("Contributor"))
    {
        await roleManager.CreateAsync(new IdentityRole("Contributor"));
    }

    // Seed Admin user
    var user1 = await userManager.FindByEmailAsync("a@a.a");
    if (user1 == null)
    {
        user1 = new ApplicationUser
        {
            UserName = "a@a.a",
            FirstName = "Admin",
            LastName = "User",
            IsApproved = true
        };
        var result = await userManager.CreateAsync(user1, "P@$$w0rd");
        if (result.Succeeded)
        {
            // Check if user1 is already in the Admin role
            if (!await userManager.IsInRoleAsync(user1, "Admin"))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(user1, "Admin");
                if (addToRoleResult.Succeeded)
                {
                    Console.WriteLine("User1 added to Admin role");
                }
            }
        }
    }


    // Seed Contributor user
    var user2 = await userManager.FindByEmailAsync("c@c.c");
    if (user2 == null)
    {
        user2 = new ApplicationUser
        {
            UserName = "c@c.c",
            FirstName = "Contributor",
            LastName = "User",
            IsApproved = false
        };
        var result = await userManager.CreateAsync(user2, "P@$$w0rd");
        if (result.Succeeded)
        {
            // Check if user2 is already in the Contributor role
            if (!await userManager.IsInRoleAsync(user2, "Contributor"))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(user2, "Contributor");
                if (addToRoleResult.Succeeded)
                {
                    await signInManager.RefreshSignInAsync(user2); // Refresh the sign-in here
                    Console.WriteLine("User2 added to Contributor role");
                }
                else
                {
                    foreach (var error in addToRoleResult.Errors)
                    {
                        Console.WriteLine($"Error adding to role: {error.Description}");
                    }
                }
            }
        }
        else
        {
            foreach (var error in result.Errors)
                Console.WriteLine($"Error creating user: {error.Description}");
        }
    }
}

// Configure middleware to handle requests
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();  // Shows detailed error pages in development mode
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();  // Add authentication middleware
app.UseAuthorization();   // Add authorization middleware

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
builder.Logging.AddConsole();
app.Run();
