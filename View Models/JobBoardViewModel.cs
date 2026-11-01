using JobTracker.Models;
namespace JobTracker.ViewModels;

// One Kanban column: every job currently in a given status.
public class JobBoardColumn
{
    public required string Title {get; init;}
    public required JobApplicationStatus Status {get; init;}
    public required List<JobApplication> Jobs {get; init;}
}

// The dashboard: the active pipeline as columns, plus closed jobs (rejected/withdrawn) listed separately.
public class JobBoardViewModel
{
    public required List<JobBoardColumn> Columns {get; init;}
    public required List<JobApplication> Closed {get; init;}
}
