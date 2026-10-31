namespace JobTracker.Models;
public class JobApplication
{
    public long ID {get; set;}
    public required string Title {get; set;}
    public string? PostingURL {get; set;}
    public string? Location {get; set;}
    public JobApplicationStatus Status {get; set;}
    public DateOnly CreatedAt {get; set;}   // The date this application was entered into our system.
    public DateOnly UpdatedAt {get; set;}   // The date this application's status was last updated at.
    public DateOnly? PostedAt {get; set;}   // The date this role was posted by the employer.
    public DateOnly? AppliedAt {get; set;}  // The date this application was submitted to the employer.
                                            // Defaults to CreatedAt.
    public required int CompanyID {get; set;}
    public Company Company {get; set;} = null!;
}