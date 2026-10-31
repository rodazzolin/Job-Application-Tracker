using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Models;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Controllers;

public class JobApplicationsController : Controller
{
    private readonly AppDbContext context;
    public JobApplicationsController(AppDbContext ctx) : base()
    {
        context = ctx;   
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var jobApplications = await context.Applications.Include(x => x.Company).ToListAsync();
        return View(jobApplications);
    }
}