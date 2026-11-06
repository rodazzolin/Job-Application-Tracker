using System.Security.Claims;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Controllers;

// Read-only pages for browsing jobs by company. Companies are created and deleted
// automatically by JobApplicationsController, so there's nothing to edit here.
[Authorize]
public class CompaniesController : Controller
{
    private readonly AppDbContext context;
    private string UserID => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public CompaniesController(AppDbContext ctx)
    {
        context = ctx;
    }

    // Lists the user's companies as cards, most recently active first.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // One SQL query: each Count/Max below becomes a subquery, so no jobs are loaded into memory.
        // Companies without jobs are skipped: they can be left behind when an edit moves a job
        // to another company (see CLAUDE.md, Domain rules).
        List<CompanyCardViewModel> companies = await context.Companies
            .Where(c => c.UserID == UserID && c.Applications.Any())
            .Select(c => new CompanyCardViewModel
            {
                ID = c.ID,
                Name = c.Name,
                Counts = new CompanyStatusCounts
                {
                    Saved = c.Applications.Count(a => a.Status == JobApplicationStatus.Saved),
                    Applied = c.Applications.Count(a => a.Status == JobApplicationStatus.Applied),
                    Interviewing = c.Applications.Count(a => a.Status == JobApplicationStatus.Interviewing),
                    Offers = c.Applications.Count(a => a.Status == JobApplicationStatus.OfferReceived),
                    Archived = c.Applications.Count(a => a.Status == JobApplicationStatus.Archived),
                },
                LastActivity = c.Applications.Max(a => a.UpdatedAt),
            })
            .OrderByDescending(c => c.LastActivity)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return View(new CompanyIndexViewModel { Companies = companies });
    }

    // Shows one company and its jobs. Filters come from the query string,
    // e.g. /Companies/Details/5?status=Applied&search=backend&sort=Title.
    [HttpGet]
    public async Task<IActionResult> Details(int id, string? search, JobApplicationStatus? status, JobBoardSort sort = JobBoardSort.Updated)
    {
        // Ignore unknown values from a hand-edited URL instead of failing, like the board does.
        if(ModelState.GetFieldValidationState(nameof(status)) == ModelValidationState.Invalid
            || (status is not null && !Enum.IsDefined(status.Value)))
            status = null;
        if(ModelState.GetFieldValidationState(nameof(sort)) == ModelValidationState.Invalid || !Enum.IsDefined(sort))
            sort = JobBoardSort.Updated;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        // The company must exist and belong to this user.
        // Selecting into an anonymous type (`new { ... }`) fetches just these values, not the whole entity.
        var company = await context.Companies
            .Where(c => c.ID == id && c.UserID == UserID)
            .Select(c => new { c.ID, c.Name, LastActivity = c.Applications.Max(a => (DateTime?)a.UpdatedAt) })
            .FirstOrDefaultAsync();
        if(company is null || company.LastActivity is null)
            return NotFound();   // No jobs: an orphaned company, hidden from the Companies page too.

        // Load all of the company's jobs (a company has few), sorted in SQL.
        List<JobCardViewModel> allJobs = await context.JobApplications
            .Where(j => j.CompanyID == id && j.UserID == UserID)
            .ApplySort(sort)
            .Select(JobCardViewModel.FromJob)
            .ToListAsync();

        // Filter in memory, so the numbers above the list still cover every job.
        // OrdinalIgnoreCase: C# string comparison is case-sensitive by default (SQL Server's isn't).
        IEnumerable<JobCardViewModel> shown = allJobs;
        if(status is not null)
            shown = shown.Where(j => j.Status == status);
        if(search is not null)
            shown = shown.Where(j => j.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                                  || (j.Location?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

        CompanyDetailsViewModel model = new()
        {
            ID = company.ID,
            Name = company.Name,
            LastActivity = company.LastActivity.Value,
            Counts = CompanyStatusCounts.FromCards(allJobs),
            Summary = JobBoardSummary.FromCards(allJobs, DateOnly.FromDateTime(DateTime.Today)),
            Search = search,
            Status = status,
            Sort = sort,
            Jobs = shown.ToList(),
        };
        return View(model);
    }
}
