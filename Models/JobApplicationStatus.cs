namespace JobTracker.Models;

public enum JobApplicationStatus
{
    Applied = 0, 
    Screening = 1,
    Interviewing = 2,
    Offered = 3,
    Rejected = 4,
    Withdrawn = 5,
    Waitlisted = 6,
}