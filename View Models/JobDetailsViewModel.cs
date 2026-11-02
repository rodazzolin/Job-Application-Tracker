using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JobTracker.Helpers;
using JobTracker.Models;
namespace JobTracker.ViewModels;

// One entry in the Details page's history timeline.
public class JobTimelineEntry
{
    public JobApplicationStatus? FromStatus {get; init;}   // Null for the first entry ("Added as …").
    public required JobApplicationStatus Status {get; init;}
    [DataType(DataType.DateTime)]
    public required DateTime Time {get; init;}             // UTC.
}

// Everything the Details page shows for one job.
public class JobDetailsViewModel
{
    public required int ID {get; init;}
    [DisplayName("Job Title")]
    public required string Title {get; init;}
    [DisplayName("Company")]
    public required string CompanyName {get; init;}
    public required JobApplicationStatus Status {get; init;}
    public string? Location {get; init;}
    [DisplayName("Posting URL")]
    public string? PostingURL {get; init;}
    [DataType(DataType.Date)]
    [DisplayName("Posted")]
    public DateOnly? PostedAt {get; init;}
    [DataType(DataType.Date)]
    [DisplayName("Applied")]
    public DateOnly? AppliedAt {get; init;}
    [DataType(DataType.DateTime)]
    [DisplayName("Added")]
    public required DateTime CreatedAt {get; init;}        // UTC.
    [DataType(DataType.DateTime)]
    [DisplayName("Last updated")]
    public required DateTime UpdatedAt {get; init;}        // UTC.

    // Newest first. Always has at least one entry: when the job was added.
    public required List<JobTimelineEntry> Timeline {get; init;}

    // When the current status began: the newest timeline entry.
    public DateTime StatusSince => Timeline[0].Time;

    // "since today" / "for 12 days".
    [DisplayName("Time in current status")]
    public string StatusDuration
    {
        get
        {
            int days = DateDisplay.DaysSince(StatusSince);
            return days == 0 ? "since today" : $"for {DateDisplay.DayCount(days)}";
        }
    }

    // The posting's site without "www.", e.g. "job-boards.greenhouse.io". Null if there's no valid URL.
    public string? PostingHost =>
        Uri.TryCreate(PostingURL, UriKind.Absolute, out Uri? uri)
            ? (uri.Host.StartsWith("www.") ? uri.Host[4..] : uri.Host)
            : null;

    // Builds the view model from a job loaded with its Company and StatusChanges.
    public static JobDetailsViewModel FromJob(JobApplication job)
    {
        List<JobApplicationStatusChange> changes = job.StatusChanges.OrderBy(s => s.Time).ToList();

        // The first entry is the job being added. Its status is the first change's OldStatus,
        // or the current status if it never changed (see CLAUDE.md, Domain rules).
        List<JobTimelineEntry> timeline =
        [
            new() { Status = changes.Count > 0 ? changes[0].OldStatus : job.Status, Time = job.CreatedAt },
            .. changes.Select(c => new JobTimelineEntry { FromStatus = c.OldStatus, Status = c.NewStatus, Time = c.Time }),
        ];
        timeline.Reverse();

        return new()
        {
            ID = job.ID,
            Title = job.Title,
            CompanyName = job.Company.Name,
            Status = job.Status,
            Location = job.Location,
            PostingURL = job.PostingURL,
            PostedAt = job.PostedAt,
            AppliedAt = job.AppliedAt,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            Timeline = timeline,
        };
    }
}
