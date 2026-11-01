using Microsoft.AspNetCore.Mvc;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;
using JobTracker.ViewModels;
using JobTracker.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using JobTracker.Services;

namespace JobTracker.Controllers;

[Authorize]
public class JobApplicationsController : Controller
{
    private readonly AppDbContext context;
    private readonly JobApplicationService jobService;
    private string UserID => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public JobApplicationsController(AppDbContext ctx, JobApplicationService svr) : base()
    {
        context = ctx;   
        jobService = svr;
    }

    // Shows the user's jobs as a Kanban board, most recently updated first in each column.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Fetches job applications for the user, sorted by newest.
        List<JobApplication> jobApplications = await context.JobApplications
            .Where(x => x.UserID == UserID)
            .Include(x => x.Company)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        // In-line helper to filter jobs by status.
        List<JobApplication> InStatus(params JobApplicationStatus[] statuses) =>
            jobApplications.Where(j => statuses.Contains(j.Status)).ToList();

        // In-line helper to create a column for the board.
        JobBoardColumn Column(string title, JobApplicationStatus status) =>
            new() { Title = title, Status = status, Jobs = InStatus(status) };

        // Instantiates the view model for the board and populates it with columns and jobs.
        JobBoardViewModel board = new()
        {
            Columns =
            [
                Column("Saved", JobApplicationStatus.Saved),
                Column("Applied", JobApplicationStatus.Applied),
                Column("Interviewing", JobApplicationStatus.Interviewing),
                Column("Offers", JobApplicationStatus.OfferReceived),
            ],
            Closed = InStatus(JobApplicationStatus.Rejected, JobApplicationStatus.Withdrawn),
        };
        return View(board);
    }

    // Creates a job application form for later submission.
    [HttpGet]
    public IActionResult Create()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        JobApplicationFormViewModel form = new() {AppliedAt = today, PostedAt = today};
        return View(form);
    }

    // Attempts to create a job application with the given form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobApplicationFormViewModel form)
    {
        // If form contains invalid data, show the same form again.
        if(!ModelState.IsValid)
            return View(form);
        
        // Fetch or create the specified company.
        string companyName = form.CompanyName.Trim();
        Company? company = await jobService.FetchOrCreateCompanyAsync(UserID, companyName);

        // Create the job app.
        DateTime now = DateTime.UtcNow;
        JobApplication jobApp = new()
        {
            UserID = UserID,
            Company = company,
            Title = form.Title,
            Location = form.Location,
            Status = form.Status,
            PostingURL = form.PostingURL,
            AppliedAt = form.Status.Equals(JobApplicationStatus.Saved) ? null : form.AppliedAt,
            PostedAt = form.PostedAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Save changes.
        context.JobApplications.Add(jobApp);
        await context.SaveChangesAsync();

        // Return to the list page.
        return RedirectToAction(nameof(Index));
    }

    // Attempts to fetch a job application for editing, returning a filled job form on success.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        JobApplication? jobApp = await context.JobApplications.Where(x => x.UserID == UserID).Include(j => j.Company).FirstOrDefaultAsync(j => j.ID == id);
        if(jobApp is null)
            return NotFound();
        
        JobApplicationFormViewModel form = new()
        {
            Title = jobApp.Title,
            PostingURL = jobApp.PostingURL,
            Location = jobApp.Location,
            PostedAt = jobApp.PostedAt,
            AppliedAt = jobApp.AppliedAt,
            CompanyName = jobApp.Company.Name,
            Status = jobApp.Status,
        };

        return View(form);
    }

    // Fetches the specified job and overwrites its fields from the form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, JobApplicationFormViewModel form)
    {
        // If form contains invalid data, show the same form again.
        if(!ModelState.IsValid)
            return View(form);

        // Fetch the job app and its company.
        JobApplication? jobApp = await context.JobApplications.Where(x => x.UserID == UserID).Include(j => j.Company).FirstOrDefaultAsync(j => j.ID == id);
        if(jobApp is null)
            return NotFound();

        // Fetch or create the specified company.
        string companyName = form.CompanyName.Trim();
        Company? company = await jobService.FetchOrCreateCompanyAsync(UserID, companyName);

        // Overwrite fields from form.
        jobApp.Title = form.Title;
        jobApp.Company = company;
        jobApp.Location = form.Location;
        jobApp.PostingURL = form.PostingURL;
        jobApp.PostedAt = form.PostedAt;

        // Change status (logs a StatusChange row if it differs), then let the form's AppliedAt win.
        jobService.ChangeStatus(jobApp, form.Status);
        jobApp.AppliedAt = form.Status.Equals(JobApplicationStatus.Saved) ? null : form.AppliedAt;

        // Increment timestamp.
        jobApp.UpdatedAt = DateTime.UtcNow;

        // Save changes.
        await context.SaveChangesAsync();

        // Return to the list page.
        return RedirectToAction(nameof(Index));
    }

    // Changes a job's status from the board's drag-and-drop. Called by JavaScript, so it returns
    // status codes (204 / 400 / 404) instead of a view or redirect.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(JobStatusUpdateViewModel input)
    {
        if(!ModelState.IsValid || !Enum.IsDefined(input.Status))
            return BadRequest();

        JobApplication? jobApp = await context.JobApplications.FirstOrDefaultAsync(j => j.ID == input.ID && j.UserID == UserID);
        if(jobApp is null)
            return NotFound();

        jobService.ChangeStatus(jobApp, input.Status);
        await context.SaveChangesAsync();
        return NoContent();
    }

    // Deletes the specified job app.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        JobApplication? jobApp = await context.JobApplications.FirstOrDefaultAsync(j => j.ID == id && j.UserID == UserID);
        if(jobApp is null)
            return NotFound();
        
        context.JobApplications.Remove(jobApp);

        // Deletes company if it just became orphaned.
        bool hasOtherJobs = await context.JobApplications.AnyAsync(j => j.CompanyID == jobApp.CompanyID && j.ID != jobApp.ID);
        if(!hasOtherJobs)
        {
            Company? company = await context.Companies.FindAsync(jobApp.CompanyID);            
            context.Companies.Remove(company!);
        }
            
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        JobApplication? jobApp = 
            await context.JobApplications.Include(j => j.Company)
            .Include(j => j.StatusChanges.OrderBy(s => s.Time))
            .FirstOrDefaultAsync(j => j.ID == id && j.UserID == UserID);
        if(jobApp is null)
            return NotFound();
        return View(jobApp);
    }
}