using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Services;
using JobTracker.Services.JobPostings;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddScoped<JobApplicationService>();
builder.Services.AddHttpClient<IJobPostingReader, GreenhouseReader>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
// Unhandled exceptions: in Development, show the detailed developer page; otherwise, re-run the
// request through ErrorController so users see a friendly page instead of a stack trace.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Empty error responses (e.g. NotFound()) get the same error page, with the status code kept.
// Responses that already have a body, like BadRequest("Invalid URL"), are left alone.
app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication(); // Must precede authorization.
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=JobApplications}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages();
app.Run();