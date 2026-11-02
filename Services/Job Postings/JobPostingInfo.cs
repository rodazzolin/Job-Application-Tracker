namespace JobTracker.Services.JobPostings;

// Defines the extractable data from a job posting URL.
public class JobPostingInfo
{
    public string? Title {get; set;}
    public string? CompanyName {get; set;}
    public string? Location {get; set;}
    public DateOnly? PostedAt {get; set;}
    public string? PostingURL {get; set;}
}