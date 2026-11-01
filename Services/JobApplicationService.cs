using JobTracker.Data;
using Microsoft.EntityFrameworkCore;
using JobTracker.Models;

namespace JobTracker.Services;

public class JobApplicationService
{
    private readonly AppDbContext context;
    public JobApplicationService(AppDbContext ctx)
    {
        context = ctx;   
    }
    
    public async Task<Company> FetchOrCreateCompanyAsync(string userID, string companyName)
    {
        Company? company = await context.Companies.Where(x => x.UserID == userID).FirstOrDefaultAsync(x => x.Name == companyName);
        if(company is null)
            company = new() {Name = companyName, UserID = userID};
        return company;
    }

    // Moves a job to a new status, logging the change and applying the AppliedAt rule.
    // Does nothing if the status is unchanged. The caller saves changes.
    public void ChangeStatus(JobApplication jobApp, JobApplicationStatus newStatus)
    {
        JobApplicationStatus oldStatus = jobApp.Status;
        if(oldStatus == newStatus)
            return;

        DateTime now = DateTime.UtcNow;
        context.StatusChanges.Add(new()
        {
            JobApplication = jobApp,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Time = now
        });

        jobApp.Status = newStatus;
        if(newStatus == JobApplicationStatus.Saved)
            jobApp.AppliedAt = null;
        else if(jobApp.AppliedAt is null)
            jobApp.AppliedAt = DateOnly.FromDateTime(DateTime.Today);
        jobApp.UpdatedAt = now;
    }
}