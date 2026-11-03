using JobTracker.Data;
using JobTracker.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using System.Net;
using JobTracker.Models;

namespace JobTracker.Tests.Integration;

[Collection("Integration")]
public class JobApplicationsIsolationTests(JobTrackerFactory factory) : IClassFixture<JobTrackerFactory>
{
    // Helper method to seed a user and a job application they own,
    // then return the job ID.
    private async Task<int> SeedJobAsync(string userID)
    {
        using var scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new IdentityUser { Id = userID, UserName = userID });
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
        // Arrange: put a job owned by user A in the database.
        string user = Guid.NewGuid().ToString();
        int jobID = await SeedJobAsync(user);

        // Instantiate a client as user A, who should be able to edit their own job.
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, user);

        // Act: attempt to edit the job posting owned by user A.
        var response = await client.GetAsync($"/JobApplications/Edit/{jobID}");

        // Assert that the response is a 200 OK.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}