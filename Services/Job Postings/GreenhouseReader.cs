using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace JobTracker.Services.JobPostings;

// Reader for Greenhouse job postings. (https://greenhouse.io/)
public class GreenhouseReader : IJobPostingReader
{
    private readonly HttpClient httpClient;
    private readonly string[] GreenhouseHosts = ["boards.greenhouse.io", "job-boards.greenhouse.io"];
    
    // Parsed location data.
    private struct GreenhouseLocation
    {
        public string? Name {get; init;}
    }

    // Internal record for the JSON structure of a Greenhouse job posting.
    // Field names must match JSON property names exactly; case auto-converted from snake to pascal.
    private record GreenhouseJobPosting()
    {
        public string? Title {get; init;}
        public string? CompanyName {get; init; }
        public GreenhouseLocation? Location {get; init;}
        public DateTimeOffset? FirstPublished {get; init;}
        public string? AbsoluteURL {get; init;}
    }

    public GreenhouseReader(HttpClient cl)
    {
        httpClient = cl;
    }

    public async Task<JobPostingInfo?> TryReadPostingAsync(Uri url)
    {
        // Checks if the URL host is one of Greenhouse's, and if the
        // path follows the convention of /{company_name}/jobs/{job_id}.
        if(!GreenhouseHosts.Contains(url.Host))
            return null;
        string[] segments = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if(segments.Length < 3 || segments[1] != "jobs" || !long.TryParse(segments[2], out long jobID))
            return null;
        string board = segments[0]; 

        // Constructs the API URL for the job posting JSON data.
        string apiURL = $"https://boards-api.greenhouse.io/v1/boards/{Uri.EscapeDataString(board)}/jobs/{jobID}";

        // Fetches the JSON data from the Greenhouse job posting URL.
        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        GreenhouseJobPosting? job = null;
        try
        {
            job = await httpClient.GetFromJsonAsync<GreenhouseJobPosting>(apiURL, options);
        }
        catch
        {
            return null;
        }
        if(job is null)
            return null;
        
        // Contructs the JobPostingInfo object from the parsed data.
        JobPostingInfo info = new()
        {
            Title = job.Title,
            CompanyName = job.CompanyName,
            Location = job.Location?.Name,
            PostedAt = job.FirstPublished is null ? null : DateOnly.FromDateTime(job.FirstPublished.Value.DateTime),
            PostingURL = job.AbsoluteURL
        };
        return info;
    }
}