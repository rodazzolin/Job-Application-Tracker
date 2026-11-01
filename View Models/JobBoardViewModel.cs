using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

    [DisplayName("Time in current status")]
    public string StatusAgeLabel
    {
        get
        {
            int days = Math.Max(0, (DateTime.UtcNow - StatusSince).Days);
            return days switch
            {
                0 => "Today",
                1 => "1 day",
                _ => $"{days} days",
            };
        }
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
}
