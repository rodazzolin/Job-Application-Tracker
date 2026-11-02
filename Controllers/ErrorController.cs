using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Models;

namespace JobTracker.Controllers;

// Shows the error page. Two pieces of middleware in Program.cs send requests here:
// - UseExceptionHandler: an unhandled exception (no code; treated as 500).
// - UseStatusCodePagesWithReExecute: an empty error response, such as NotFound() (?code=404).
// There's deliberately no [HttpGet]: the original request is re-run here with its own method,
// so a failed POST must reach this action too.
[AllowAnonymous]
public class ErrorController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index(int? code)
    {
        int statusCode = code ?? StatusCodes.Status500InternalServerError;
        Response.StatusCode = statusCode;

        (string title, string message) = statusCode switch
        {
            404 => ("Page not found", "That page doesn't exist, or the job you're looking for was deleted."),
            400 => ("Bad request", "Something about that request wasn't right. Please go back and try again."),
            403 => ("Access denied", "You don't have permission to view this page."),
            >= 500 => ("Something went wrong", "An unexpected error happened on our end. Please try again in a moment."),
            _ => ("Something went wrong", "That request couldn't be completed."),
        };

        // Named explicitly: by default View() looks for a view named after the action ("Index").
        return View("Error", new ErrorViewModel
        {
            StatusCode = statusCode,
            Title = title,
            Message = message,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
        });
    }
}
