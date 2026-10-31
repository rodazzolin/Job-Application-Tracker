using Microsoft.AspNetCore.Mvc;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;
using JobTracker.ViewModels;
using JobTracker.Models;

namespace JobTracker.Controllers;

public class JobApplicationsController : Controller
{
    private readonly AppDbContext context;
    public JobApplicationsController(AppDbContext ctx) : base()
    {
        context = ctx;   
    }

    // Returns a list of all job applications.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var jobApplications = await context.JobApplications.Include(x => x.Company).ToListAsync();
        return View(jobApplications);
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
        Company? company = await context.Companies.FirstOrDefaultAsync(x => x.Name == companyName);
        if(company is null)
            company = new() {Name = companyName};

        // Create the job app.
        DateTime now = DateTime.UtcNow;
        JobApplication jobApp = new()
        {
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
        JobApplication? jobApp = await context.JobApplications.Include(j => j.Company).FirstOrDefaultAsync(j => j.ID == id);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, JobApplicationFormViewModel form)
    {
        // If form contains invalid data, show the same form again.
        if(!ModelState.IsValid)
            return View(form);

        // Fetch the job app and its company.
        JobApplication? jobApp = await context.JobApplications.Include(j => j.Company).FirstOrDefaultAsync(j => j.ID == id);
        if(jobApp is null)
            return NotFound();

        // Fetch or create the specified company.
        string companyName = form.CompanyName.Trim();
        Company? company = await context.Companies.FirstOrDefaultAsync(x => x.Name == companyName);
        if(company is null)
            company = new() {Name = companyName};

        // Overwrite fields from form.
        jobApp.Title = form.Title;
        jobApp.Company = company;
        jobApp.Location = form.Location;
        jobApp.Status = form.Status;
        jobApp.PostingURL = form.PostingURL;
        jobApp.PostedAt = form.PostedAt;
        jobApp.AppliedAt = form.Status.Equals(JobApplicationStatus.Saved) ? null : form.AppliedAt;

        // Increment timestamp.
        jobApp.UpdatedAt = DateTime.UtcNow;

        // Save changes.
        await context.SaveChangesAsync();

        // Return to the list page.
        return RedirectToAction(nameof(Index));
    }
}