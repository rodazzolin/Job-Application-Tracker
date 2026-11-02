namespace JobTracker.Models;
public class JobApplicationStatusChange
{
    public int ID {get; set;}
    public int JobApplicationID {get; set;}
    public JobApplication JobApplication {get; set;} = null!;
    public JobApplicationStatus OldStatus {get; set;}
    public JobApplicationStatus NewStatus {get; set;}
    public DateTime Time {get; set;}
}