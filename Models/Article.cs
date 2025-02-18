// Jack Gerber - A01266976
// Date: Feb 16, 2025

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlogApp.Models;

public class Article
{
    [Key]
    public int ArticleId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; }

    [Required]
    public string Body { get; set; }

    public DateTime CreateDate { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [BindNever]
    [Required]
    [EmailAddress]  // Ensure email format is correct
    public string ContributorUsername { get; set; } // Store the email (username) of the contributor
}

