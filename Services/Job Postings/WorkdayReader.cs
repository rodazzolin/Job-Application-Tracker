namespace JobTracker.Services.JobPostings;
using System.Text.Json;

public class WorkdayReader : IJobPostingReader
{
    private readonly HttpClient httpClient;
    private readonly string WorkdayHostEnd = ".myworkdayjobs.com";

    // Parsed company data.
    private struct WorkdayCompany
    {
        public string? Name {get; init;}
    }

    // Internal record for the JSON structure of a Workday job posting.
    private record WorkdayRecord()
    {
        public WorkdayJobPostingInfo? JobPostingInfo {get; init;}
        public WorkdayCompany? HiringOrganization {get; init;}
    }
    private record WorkdayJobPostingInfo()
    {
        public string? Title {get; init;}
        public string? Location {get; init;}
        public DateOnly? StartDate {get; init;}
        public string? ExternalURL {get; init;}
    }

    public WorkdayReader(HttpClient cl)
    {
        httpClient = cl;
    }

    public async Task<JobPostingInfo?> TryReadPostingAsync(Uri url)
    {
        // Checks if the URL host is one of Workday's, and if the
        // path follows the convention of /{site}/job/{location}/{job name}_{ref id}
        if(!url.Host.EndsWith(WorkdayHostEnd, StringComparison.OrdinalIgnoreCase))
            return null;
        string[] segments = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int jobIndex = segments.IndexOf("job");
        if(jobIndex < 1 || jobIndex >= segments.Length - 1)
            return null; // "job" must be preceded by the site and followed by fields.
        
        // Constructs the API URL for the job posting JSON data,
        // using the convention of /wday/cxs/{tenant}/{site}/job/{the rest}.
        // Not all links have the same segments, so we take them as they come.
        string tenant = url.Host.Split('.')[0];
        string site = segments[jobIndex - 1];
        string rest = string.Join('/', segments[(jobIndex + 1)..]);
        string apiURL = $"https://{url.Host}/wday/cxs/{tenant}/{site}/job/{rest}";

        // Fetches the JSON data from the Workday job posting URL.
        WorkdayRecord? record = null;
        try
        {
            record = await httpClient.GetFromJsonAsync<WorkdayRecord>(apiURL);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is NotSupportedException || ex is JsonException)
        {
            return null;
        }
        if(record is null)
            return null;

        return new JobPostingInfo()
        {
            Title = record.JobPostingInfo?.Title,
            CompanyName = record.HiringOrganization?.Name,
            Location = record.JobPostingInfo?.Location,
            PostedAt = record.JobPostingInfo?.StartDate,
            PostingURL = record.JobPostingInfo?.ExternalURL
        };
    }
}