using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace JobTracker.Tests.Integration;

[Collection("Integration")]
public class CompaniesIsolationTests(JobTrackerFactory factory) : IClassFixture<JobTrackerFactory>
{
    // Seeds a user with one company and one job there, and returns the company ID.
    private async Task<int> SeedCompanyAsync(string userID, string companyName)
    {
        using var scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new IdentityUser { Id = userID, UserName = userID });
        Company company = new() { Name = companyName, UserID = userID };
        db.JobApplications.Add(new JobApplication { Title = "Job A", UserID = userID, Company = company });
        await db.SaveChangesAsync();
        return company.ID;
    }

    private HttpClient ClientFor(string userID)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userID);
        return client;
    }

    [Fact]
    public async Task OtherUsersCannotViewACompany()
    {
        string userA = Guid.NewGuid().ToString();
        string userB = Guid.NewGuid().ToString();
        int companyID = await SeedCompanyAsync(userA, "Company A");

        var response = await ClientFor(userB).GetAsync($"/Companies/Details/{companyID}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserCanViewTheirOwnCompany()
    {
        string user = Guid.NewGuid().ToString();
        int companyID = await SeedCompanyAsync(user, "Company A");

        var response = await ClientFor(user).GetAsync($"/Companies/Details/{companyID}?status=Bogus&sort=Bogus");

        // Unknown filter values fall back to defaults instead of failing.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompaniesPageListsOnlyTheUsersCompanies()
    {
        string userA = Guid.NewGuid().ToString();
        string userB = Guid.NewGuid().ToString();
        string nameA = $"Mine {Guid.NewGuid()}";
        string nameB = $"Theirs {Guid.NewGuid()}";
        await SeedCompanyAsync(userA, nameA);
        await SeedCompanyAsync(userB, nameB);

        string html = await ClientFor(userA).GetStringAsync("/Companies");

        Assert.Contains(nameA, html);
        Assert.DoesNotContain(nameB, html);
    }
}
