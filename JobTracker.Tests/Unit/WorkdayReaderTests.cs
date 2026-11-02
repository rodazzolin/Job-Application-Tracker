using System.Net;
using JobTracker.Services.JobPostings;
using JobTracker.Tests.Helpers;
namespace JobTracker.Tests.Unit;

public class WorkdayReaderTests
{
    private const string UCFPostingURL = "https://ucf.wd1.myworkdayjobs.com/careers/job/Orlando-FL-Main-Campus/IT-Client-Support-Specialist-I_R114229/";
    private const string UCFExternalPostingURL = "https://ucf.wd1.myworkdayjobs.com/careers/job/Orlando-FL-Main-Campus/IT-Client-Support-Specialist-I_R114229";
    private const string UCFPostingAPIURL = "https://ucf.wd1.myworkdayjobs.com/wday/cxs/ucf/careers/job/Orlando-FL-Main-Campus/IT-Client-Support-Specialist-I_R114229";
    [Theory]
    [InlineData("https://example.com/careers/job/Foo_R123")]                // Wrong host.
    [InlineData("https://ucf.wd1.myworkdayjobs.com/careers/Foo_R123")]      // No "job" segment.
    [InlineData("https://ucf.wd1.myworkdayjobs.com/careers/job")]           // Nothing after "job".
    [InlineData("https://ucf.wd1.myworkdayjobs.com/job/Foo_R123")]          // "job" not preceded by site.
    public async Task ReturnsNullForInvalidURLs(string url)
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        var reader = new WorkdayReader(new HttpClient(handler));

        var info = await reader.TryReadPostingAsync(new Uri(url));

        // Checks that the reader returns null and a request was never made for URLs it cannot handle.
        Assert.Null(info);
        Assert.Null(handler.LastRequestUri);  
    }

    [Fact]
    public async Task ReadUCFPosting()
    {
        var handler = new StubHandler(HttpStatusCode.OK, File.ReadAllText("Fixtures/Workday/ucf_posting.json"));
        var reader = new WorkdayReader(new HttpClient(handler));

        var info = await reader.TryReadPostingAsync(new Uri(UCFPostingURL));
        Assert.NotNull(info);
        Assert.Equal(UCFPostingAPIURL, handler.LastRequestUri?.ToString());
        Assert.Equal("IT Client Support Specialist I", info.Title);
        Assert.Equal("Orlando, FL (Main Campus)", info.Location);
        Assert.Equal("The University of Central Florida Board of Trustees", info.CompanyName);
        Assert.Equal(new DateOnly(2026, 10, 29), info.PostedAt);
        Assert.Equal(UCFExternalPostingURL, info.PostingURL);
    }

    [Fact]
    public async Task ReturnsNullWhenWorkdayReturnsAnError()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound, "");
        var reader = new WorkdayReader(new HttpClient(handler));

        var info = await reader.TryReadPostingAsync(new Uri(UCFPostingURL));

        Assert.Null(info);
    }

    [Fact]
    public async Task ReturnsNullWhenWorkdayReturnsNonJSON()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "not json");
        var reader = new WorkdayReader(new HttpClient(handler));

        var info = await reader.TryReadPostingAsync(new Uri(UCFPostingURL));

        Assert.Null(info);
    }
}
