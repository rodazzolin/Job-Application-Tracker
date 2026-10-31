namespace JobTracker.Models;

public class JobApplication
{
    public long ID {get; set;}
    public required string Title {get; set;}
    public DateOnly Date {get; set;}
    public JobApplicationStatus Status {get; set;}
    public required int CompanyID {get; set;}
    public Company? Company {get; set;}
}