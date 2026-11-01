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

}