using Microsoft.AspNetCore.Mvc;
using JobTracker.Data;
using JobTracker.Helpers;
using Microsoft.EntityFrameworkCore;
using JobTracker.ViewModels;
using JobTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using JobTracker.Services;
using JobTracker.Services.JobPostings;

namespace JobTracker.Controllers;

[Authorize]
public class JobApplicationsController : Controller
{
    private readonly AppDbContext context;
    private readonly JobApplicationService jobService;
    private readonly IEnumerable<IJobPostingReader> readers;
    private string UserID => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // Blank or whitespace-only notes are stored as null, so "no notes" has one representation.
    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.TrimEnd();

    public JobApplicationsController(AppDbContext ctx, JobApplicationService svr, IEnumerable<IJobPostingReader> rd) : base()
    {
        context = ctx;   
        jobService = svr;
        readers = rd;
    }

    // Shows the user's jobs as a Kanban board.
    // Both options come from the query string (e.g. ?range=LastMonth&sort=Title):
    // `range` limits jobs by UpdatedAt, `sort` orders the cards within each column.
    [HttpGet]
    public async Task<IActionResult> Index(JobBoardRange range = JobBoardRange.Last3Months, JobBoardSort sort = JobBoardSort.Updated)
    {
        // Fall back to the default on an unknown value (e.g. a hand-edited URL).
        // Checked per parameter, so a bad ?sort= doesn't also reset a good ?range=.
        if(ModelState.GetFieldValidationState(nameof(range)) == ModelValidationState.Invalid || !Enum.IsDefined(range))
            range = JobBoardRange.Last3Months;
        if(ModelState.GetFieldValidationState(nameof(sort)) == ModelValidationState.Invalid || !Enum.IsDefined(sort))
            sort = JobBoardSort.Updated;

        // Fetches the user's jobs, optionally limited to the selected time range.
        IQueryable<JobApplication> query = context.JobApplications.Where(x => x.UserID == UserID);
        DateTime? cutoff = range.GetCutoff(DateTime.UtcNow);
        if(cutoff is not null)
            query = query.Where(x => x.UpdatedAt >= cutoff);

        // Project straight into card view models, so SQL only selects the columns the cards use.
        // Sorting once here is enough: splitting into columns below keeps this order.
        List<JobCardViewModel> jobApplications = await query
            .ApplySort(sort)
            .Select(JobCardViewModel.FromJob)
            .ToListAsync();

        // Only worth a second query when the range shows nothing: are there jobs outside it?
        bool hasAnyJobs = jobApplications.Count > 0
            || await context.JobApplications.AnyAsync(x => x.UserID == UserID);

        // In-line helper to filter jobs by status.
        List<JobCardViewModel> InStatus(params JobApplicationStatus[] statuses) =>
            jobApplications.Where(j => statuses.Contains(j.Status)).ToList();

        // In-line helper to create a column for the board.
        JobBoardColumn Column(string title, JobApplicationStatus status) =>
            new() { Title = title, Status = status, Jobs = InStatus(status) };

        // Instantiates the view model for the board and populates it with columns and jobs.
        JobBoardViewModel board = new()
        {
            Range = range,
            Sort = sort,
            Columns =
            [
                Column("Saved", JobApplicationStatus.Saved),
                Column("Applied", JobApplicationStatus.Applied),
                Column("Interviewing", JobApplicationStatus.Interviewing),
                Column("Offers", JobApplicationStatus.OfferReceived),
            ],
            Archived = InStatus(JobApplicationStatus.Archived),
            Summary = JobBoardSummary.FromCards(jobApplications, DateOnly.FromDateTime(DateTime.Today)),
            HasAnyJobs = hasAnyJobs,
        };
        return View(board);
    }

    // Creates a job application form for later submission.
    // `status` (optional, e.g. ?status=Applied) preselects a status; the board's "+" buttons use it.
    [HttpGet]
    public IActionResult Create(JobApplicationStatus? status)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        JobApplicationFormViewModel form = new() {AppliedAt = today, PostedAt = today};
        if(status is not null && Enum.IsDefined(status.Value))
            form.Status = status.Value;

        // An unknown ?status= shouldn't show as a validation error on a fresh form.
        ModelState.Clear();
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
            Notes = NormalizeNotes(form.Notes),
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
            Notes = jobApp.Notes,
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
        jobApp.Notes = NormalizeNotes(form.Notes);

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
        if(!await jobService.DeleteAsync(UserID, id))
            return NotFound();

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Deletes a job from the board's card menu. Called by job-board.js with fetch, so it returns
    // status codes (204 / 404) instead of redirecting, like UpdateStatus.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFromBoard(int id)
    {
        if(!await jobService.DeleteAsync(UserID, id))
            return NotFound();

        await context.SaveChangesAsync();
        return NoContent();
    }

    // Fetches a job application and its status changes for viewing.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        JobApplication? jobApp = 
            await context.JobApplications.Include(j => j.Company)
            .Include(j => j.StatusChanges)
            .FirstOrDefaultAsync(j => j.ID == id && j.UserID == UserID);
        if(jobApp is null)
            return NotFound();
        return View(JobDetailsViewModel.FromJob(jobApp));
    }

    // Given a job posting URL, attempts to fetch the job's title, company, and location from the posting
    // via a number of individual readers.
    [HttpGet]
    public async Task<IActionResult> Lookup(string url)
    {
        // Attemps to parse the URL.
        if(!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return BadRequest("Invalid URL");
        
        // Attempts to read the job posting from each reader in order.
        foreach(IJobPostingReader reader in readers)
        {
            JobPostingInfo? info = await reader.TryReadPostingAsync(uri);
            if(info != null)
                return Json(info);
        }
        return NotFound();
    }
}