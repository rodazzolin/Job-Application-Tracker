namespace JobTracker.Models;
public class JobApplication
{
    public int ID {get; set;}
    public string UserID {get; set;} = "";
    public required string Title {get; set;}
    public string? PostingURL {get; set;}
    public string? Location {get; set;}
    public JobApplicationStatus Status {get; set;}
    public DateTime CreatedAt {get; set;}   // The date this application was entered into our system.
    public DateTime UpdatedAt {get; set;}   // The date this application's status was last updated at.
    public DateOnly? PostedAt {get; set;}   // The date this role was posted by the employer.
    public DateOnly? AppliedAt {get; set;}  // The date this application was submitted to the employer.
                                            // Defaults to CreatedAt.
    public int CompanyID {get; set;}
    public Company Company {get; set;} = null!;
    public List<JobApplicationStatusChange> StatusChanges {get; set;} = [];
}