using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JobTracker.Models;
namespace JobTracker.ViewModels;

// How many of a company's jobs are in each status.
public class CompanyStatusCounts
{
    public required int Saved {get; init;}
    public required int Applied {get; init;}
    public required int Interviewing {get; init;}
    public required int Offers {get; init;}
    public required int Archived {get; init;}

    // Statuses in board order (the enum's order differs), for the status bar, legend and filter.
    public static readonly JobApplicationStatus[] DisplayOrder =
    [
        JobApplicationStatus.Saved,
        JobApplicationStatus.Applied,
        JobApplicationStatus.Interviewing,
        JobApplicationStatus.OfferReceived,
        JobApplicationStatus.Archived,
    ];

    [DisplayName("Postings")]
    public int Total => Saved + Applied + Interviewing + Offers + Archived;
    [DisplayName("Active")]
    public int Active => Total - Archived;

    public int Count(JobApplicationStatus status) => status switch
    {
        JobApplicationStatus.Saved => Saved,
        JobApplicationStatus.Applied => Applied,
        JobApplicationStatus.Interviewing => Interviewing,
        JobApplicationStatus.OfferReceived => Offers,
        JobApplicationStatus.Archived => Archived,
        _ => 0,
    };

    // Counts from cards already in memory (the company page loads them anyway).
    public static CompanyStatusCounts FromCards(IReadOnlyCollection<JobCardViewModel> cards) => new()
    {
        Saved = cards.Count(c => c.Status == JobApplicationStatus.Saved),
        Applied = cards.Count(c => c.Status == JobApplicationStatus.Applied),
        Interviewing = cards.Count(c => c.Status == JobApplicationStatus.Interviewing),
        Offers = cards.Count(c => c.Status == JobApplicationStatus.OfferReceived),
        Archived = cards.Count(c => c.Status == JobApplicationStatus.Archived),
    };
}

// One card on the Companies page.
public class CompanyCardViewModel
{
    public required int ID {get; init;}
    [DisplayName("Company")]
    public required string Name {get; init;}
    public required CompanyStatusCounts Counts {get; init;}
    [DataType(DataType.DateTime)]
    [DisplayName("Last activity")]
    public required DateTime LastActivity {get; init;}   // UTC: newest UpdatedAt among its jobs.
}

// The Companies page: every company the user has at least one job with.
public class CompanyIndexViewModel
{
    public required List<CompanyCardViewModel> Companies {get; init;}
}

// One company's page: its numbers, plus its jobs narrowed by the filter form.
public class CompanyDetailsViewModel
{
    public required int ID {get; init;}
    [DisplayName("Company")]
    public required string Name {get; init;}
    [DataType(DataType.DateTime)]
    [DisplayName("Last activity")]
    public required DateTime LastActivity {get; init;}   // UTC.

    // Computed from all the company's jobs, not just the filtered ones.
    public required CompanyStatusCounts Counts {get; init;}
    public required JobBoardSummary Summary {get; init;}

    // Current filter values, echoed back into the form. Null means "any".
    [DisplayName("Search")]
    public string? Search {get; init;}
    [DisplayName("Status")]
    public JobApplicationStatus? Status {get; init;}
    [DisplayName("Sort by")]
    public required JobBoardSort Sort {get; init;}

    // The jobs that match the filters, in the selected order.
    public required List<JobCardViewModel> Jobs {get; init;}

    public bool HasFilters => Search is not null || Status is not null;
}
