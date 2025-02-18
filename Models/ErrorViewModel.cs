// Jack Gerber - A01266976
// Date: Feb 16, 2025

namespace BlogApp.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
