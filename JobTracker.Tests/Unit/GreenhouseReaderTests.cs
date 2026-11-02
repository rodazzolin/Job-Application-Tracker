using System.Net;
using JobTracker.Services.JobPostings;
using JobTracker.Tests.Helpers;
namespace JobTracker.Tests.Unit;

public class GreenhouseReaderTests
{
    private const string WarpPostingURL = "https://job-boards.greenhouse.io/warp/jobs/4324888004";
    private const string WarpPostingAPIURL = "https://boards-api.greenhouse.io/v1/boards/warp/jobs/4324888004";

    [Theory]
    [InlineData("https://invalid.com/jobs/123")]                            // Wrong host.
    [InlineData("https://job-boards.greenhouse.io/company/notjobs/123")]    // No "jobs" segment.
    [InlineData("https://job-boards.greenhouse.io/jobs/company/123")]       // "jobs" segment out of place.
    [InlineData("https://job-boards.greenhouse.io/company/jobs/")]          // No job ID.
    public async Task ReturnsNullForInvalidURLs(string url)
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        var reader = new GreenhouseReader(new HttpClient(handler));

        var info = await reader.TryReadPostingAsync(new Uri(url));

        // Checks that the reader returns null and a request was never made for URLs it cannot handle.
        Assert.Null(info);
        Assert.Null(handler.LastRequestUri);  
    }

    [Fact]
    public async Task ReadWarpPosting()
    {
        var handler = new StubHandler(HttpStatusCode.OK, File.ReadAllText("Fixtures/Greenhouse/warp_posting.json"));
        var reader = new GreenhouseReader(new HttpClient(handler));

        var info = await reader.TryReadPostingAsync(new Uri(WarpPostingURL));
        Assert.NotNull(info);
        Assert.Equal(WarpPostingAPIURL, handler.LastRequestUri?.ToString());
        Assert.Equal("Software Engineer", info.Title);
        Assert.Equal("Remote within US and Canada", info.Location);
        Assert.Equal("Warp", info.CompanyName);
        Assert.Equal(new DateOnly(2022, 2, 1), info.PostedAt);
        Assert.Equal(WarpPostingURL, info.PostingURL);
    }

    [Fact]
    public async Task ReturnsNullWhenGreenhouseReturnsAnError()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound, "");
        var reader = new GreenhouseReader(new HttpClient(handler));

        var info = await reader.TryReadPostingAsync(new Uri(WarpPostingURL));

        Assert.Null(info);
    }

    [Fact]
    public async Task ReturnsNullWhenGreenhouseReturnsNonJSON()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "not json");
        var reader = new GreenhouseReader(new HttpClient(handler));

        var info = await reader.TryReadPostingAsync(new Uri(WarpPostingURL));

        Assert.Null(info);
    }

}