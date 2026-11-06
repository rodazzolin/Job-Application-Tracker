using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
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

    // The start of the job's notes (the query fetches only the first characters), or null if none.
    public string? NotesStart {get; init;}
    [DisplayName("Notes")]
    public string? NotesPreview => TextDisplay.Preview(NotesStart);

    [DisplayName("Time in current status")]
    public string StatusAgeLabel => DateDisplay.StatusAge(StatusSince);

    // The card's bottom-right label follows the board's sort: time in status for "Last updated",
    // time since applying for "Date applied" (none if never applied), and nothing for "Title".
    public string? AgeLabel(JobBoardSort sort) => sort switch
    {
        JobBoardSort.Applied => AppliedAt is DateOnly applied ? DateDisplay.DateAge(applied) : null,
        JobBoardSort.Title => null,
        _ => StatusAgeLabel,
    };

    public static string AgeTooltip(JobBoardSort sort) =>
        sort == JobBoardSort.Applied ? "Time since applied" : "Time in current status";

    // Turns a job into a card inside a query: `query.Select(JobCardViewModel.FromJob)`.
    // It's an Expression (a description of the code, not compiled code), so EF can translate it
    // into SQL and select only the columns the card uses. Shared by the board and the company page.
    public static readonly Expression<Func<JobApplication, JobCardViewModel>> FromJob = x => new JobCardViewModel
    {
        ID = x.ID,
        Title = x.Title,
        CompanyName = x.Company.Name,
        Status = x.Status,
        // Latest status change, or CreatedAt if the status never changed.
        StatusSince = x.StatusChanges.Max(s => (DateTime?)s.Time) ?? x.CreatedAt,
        Location = x.Location,
        AppliedAt = x.AppliedAt,
        // Becomes SQL SUBSTRING, so long notes aren't sent in full just for a short preview.
        NotesStart = x.Notes == null ? null : x.Notes.Substring(0, TextDisplay.CardPreviewFetchLength),
        // Becomes an EXISTS subquery in SQL, so there's still a single query.
        HasResponse = x.Status == JobApplicationStatus.Interviewing
            || x.Status == JobApplicationStatus.OfferReceived
            || x.StatusChanges.Any(s => s.NewStatus == JobApplicationStatus.Interviewing
                                     || s.NewStatus == JobApplicationStatus.OfferReceived),
    };
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
    [DisplayName("Sort cards by")]
    public required JobBoardSort Sort {get; init;}
    public required List<JobBoardColumn> Columns {get; init;}
    public required List<JobCardViewModel> Archived {get; init;}
    public required JobBoardSummary Summary {get; init;}
    // False only for a brand-new user with no jobs at all (in any range): shows the first-run message.
    public required bool HasAnyJobs {get; init;}

    // Cards shown in the selected range (board + archived).
    public int VisibleCount => Columns.Sum(c => c.Jobs.Count) + Archived.Count;
}
