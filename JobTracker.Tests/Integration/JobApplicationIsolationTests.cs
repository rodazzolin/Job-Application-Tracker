using JobTracker.Data;
using JobTracker.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using System.Net;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.RegularExpressions;

namespace JobTracker.Tests.Integration;

[Collection("Integration")]
public class JobApplicationsIsolationTests(JobTrackerFactory factory) : IClassFixture<JobTrackerFactory>
{
    // Helper method to seed a user with no jobs.
    private async Task SeedUserAsync(string userID)
    {
        using var scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new IdentityUser { Id = userID, UserName = userID });
        await db.SaveChangesAsync();
    }

    // Helper method to check whether a job with the given ID is still in the database.
    private async Task<bool> JobExistsAsync(int jobID)
    {
        using var scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.JobApplications.AnyAsync(j => j.ID == jobID);
    }

    // Loads a page and pulls the anti-forgery token out of its hidden form field.
    // The same client also receives the matching anti-forgery cookie, which it sends back on the POST.
    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string url)
    {
        string html = await client.GetStringAsync(url);
        Match match = Regex.Match(html, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        Assert.True(match.Success, $"No anti-forgery token found on {url}.");
        return match.Groups[1].Value;
    }

    // Helper method to seed a user and a job application they own,
    // then return the job ID.
    private async Task<int> SeedJobAsync(string userID)
    {
        await SeedUserAsync(userID);
        using var scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Company company = new Company { Name = "Company A", UserID = userID };
        JobApplication job = new JobApplication { Title = "Job A", UserID = userID, Company = company };
        db.Companies.Add(company);
        db.JobApplications.Add(job);
        await db.SaveChangesAsync();
        return job.ID;
    }

    [Fact]
    public async Task OtherUsersCannotEditAJob()
    {
        // Arrange: put a job owned by user A in the database.
        string userA = Guid.NewGuid().ToString();
        string userB = Guid.NewGuid().ToString();
        int jobID = await SeedJobAsync(userA);

        // Instantiate a client as user B, who should not be able to edit the job owned by user A.
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userB);

        // Act: attempt to edit the job posting owned by user A.
        var response = await client.GetAsync($"/JobApplications/Edit/{jobID}");

        // Assert that the response is a 404.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserCanEditTheirOwnJob()
    {
        // Arrange: put a job owned by the user in the database.
        string user = Guid.NewGuid().ToString();
        int jobID = await SeedJobAsync(user);

        // Instantiate a client as the user, who should be able to edit their own job.
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, user);

        // Act: attempt to edit the job posting owned by the user.
        var response = await client.GetAsync($"/JobApplications/Edit/{jobID}");

        // Assert that the response is a 200 OK.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OtherUsersCannotDeleteAJob()
    {
        // Arrange: user A owns a job; user B exists but has no jobs.
        string userA = Guid.NewGuid().ToString();
        string userB = Guid.NewGuid().ToString();
        int jobID = await SeedJobAsync(userA);
        await SeedUserAsync(userB);

        // Log in as user B and get a valid anti-forgery token from the create page,
        // so the request gets past [ValidateAntiForgeryToken] and reaches the ownership check.
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userB);
        string token = await GetAntiForgeryTokenAsync(client, "/JobApplications/Create");

        // Act: attempt to delete the job owned by user A.
        var response = await client.PostAsync($"/JobApplications/Delete/{jobID}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));

        // Assert: the response is a 404 and the job is still there.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(await JobExistsAsync(jobID));
    }

    [Fact]
    public async Task UserCanDeleteTheirOwnJob()
    {
        // Arrange: user owns a job.
        string user = Guid.NewGuid().ToString();
        int jobID = await SeedJobAsync(user);

        // Log in as the user and get a valid anti-forgery token from the create page,
        // so the request gets past [ValidateAntiForgeryToken] and reaches the ownership check.
        // Redirects are disabled so we can check the response code.
        HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, user);
        string token = await GetAntiForgeryTokenAsync(client, "/JobApplications/Create");

        // Act: attempt to delete the job owned by the user.
        var response = await client.PostAsync($"/JobApplications/Delete/{jobID}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        // Assert: the response is a redirect and the job is gone.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);   // redirected back to Index
        Assert.False(await JobExistsAsync(jobID));
    }
}