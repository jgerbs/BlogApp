// Jack Gerber - A01266976
// Date: Feb 16, 2025

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BlogApp.Models;

public class ApplicationUser : IdentityUser
{

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public bool IsApproved { get; set; } = false; // Admin approval flag
        
        public string Role { get; set; } = "Contributor"; // Default role
}
