using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JobTracker.Helpers;
using JobTracker.Models;
namespace JobTracker.ViewModels;

// What a board card needs: a flat projection of a job, plus when its current status began.
public class JobCardViewModel
{
    public required int ID {get; init;}
    [DisplayName("Job Title")]
    public required string Title {get; init;}
    [DisplayName("Company")]
    public required string CompanyName {get; init;}
    public required JobApplicationStatus Status {get; init;}
    [DataType(DataType.DateTime)]
    [DisplayName("Status Since")]
    public required DateTime StatusSince {get; init;}   // UTC: last status change, or CreatedAt if never changed.

    public string? Location {get; init;}
    [DataType(DataType.Date)]
    [DisplayName("Applied")]
    public DateOnly? AppliedAt {get; init;}
    // True if the job has ever reached Interviewing or Offer Received (now or in its history).
    [DisplayName("Got a response")]
    public required bool HasResponse {get; init;}

    [DisplayName("Time in current status")]
    public string StatusAgeLabel => DateDisplay.StatusAge(StatusSince);
}

// The numbers above the board, computed from the cards shown (so they follow the time range).
// job-board.js recomputes the same numbers after a move or delete; keep the two in sync.
public class JobBoardSummary
{
    [DisplayName("Active")]
    public required int Active {get; init;}               // Not archived.
    [DisplayName("Applied this week")]
    public required int AppliedThisWeek {get; init;}      // AppliedAt within the last 7 days, today included.
    public required int AppliedCount {get; init;}         // Jobs with an AppliedAt.
    public required int RespondedCount {get; init;}       // Of those, how many got a response.
    [DisplayName("Offers")]
    public required int Offers {get; init;}

    // Rounded percentage, or null when nothing has been applied to yet.
    [DisplayName("Response rate")]
    public int? ResponseRate => AppliedCount == 0
        ? null
        : (int)Math.Round(100.0 * RespondedCount / AppliedCount, MidpointRounding.AwayFromZero);

    public static JobBoardSummary FromCards(IReadOnlyCollection<JobCardViewModel> cards, DateOnly today)
    {
        List<JobCardViewModel> applied = cards.Where(c => c.AppliedAt is not null).ToList();
        return new()
        {
            Active = cards.Count(c => c.Status != JobApplicationStatus.Archived),
            AppliedThisWeek = applied.Count(c => c.AppliedAt >= today.AddDays(-6)),
            AppliedCount = applied.Count,
            RespondedCount = applied.Count(c => c.HasResponse),
            Offers = cards.Count(c => c.Status == JobApplicationStatus.OfferReceived),
        };
    }
}

// One Kanban column: every job currently in a given status.
public class JobBoardColumn
{
    public required string Title {get; init;}
    public required JobApplicationStatus Status {get; init;}
    public required List<JobCardViewModel> Jobs {get; init;}
}

// The dashboard: the active pipeline as columns, plus archived jobs listed separately.
public class JobBoardViewModel
{
    [DisplayName("Show jobs updated in")]
    public required JobBoardRange Range {get; init;}
    public required List<JobBoardColumn> Columns {get; init;}
    public required List<JobCardViewModel> Archived {get; init;}
    public required JobBoardSummary Summary {get; init;}
    // False only for a brand-new user with no jobs at all (in any range): shows the first-run message.
    public required bool HasAnyJobs {get; init;}

    // Cards shown in the selected range (board + archived).
    public int VisibleCount => Columns.Sum(c => c.Jobs.Count) + Archived.Count;
}
