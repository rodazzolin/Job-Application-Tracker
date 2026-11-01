using JobTracker.Models;
namespace JobTracker.ViewModels;

// Input for JobApplicationsController.UpdateStatus, posted by the board's drag-and-drop script.
public class JobStatusUpdateViewModel
{
    public int ID {get; set;}
    public JobApplicationStatus Status {get; set;}
}
